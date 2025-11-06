using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnceUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(Once);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnceUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var once = unitExporter.unit as Once;
            var onceVarName = "once"+once.guid.ToString();

            var getVar = unitExporter.CreateNode<Variable_GetNode>();

            var varIndex = unitExporter.vsExportContext.AddVariableWithIdIfNeeded(onceVarName, false, VariableKind.Flow, typeof(bool));
            getVar.Configuration["variable"].Value = varIndex;
            
            var branch = unitExporter.CreateNode<Flow_BranchNode>();
            // Branch flow in - from Once.Enter
            branch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(once.enter);
            //Condition - from GetVariable
            branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(getVar.ValueOut(Variable_GetNode.IdOutputValue));
            
            // Once.After flow to Branch when true
            branch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(once.after);

            var setVar = VariablesHelpers.SetVariable(unitExporter, varIndex, out var setVarSocket, out var setVarFlowIn, out var setVarFlowOut);
            setVarSocket.SetValue(true);
            // Set OnceVariable to true when Branch is false 
            branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).ConnectToFlowDestination(setVarFlowIn);
            // Map once.once out flow to SetVariable outflow
            setVarFlowOut.MapToControlOutput(once.once);
            
            if (once.reset.hasAnyConnection)
            {
                var resetVar = VariablesHelpers.SetVariable(unitExporter, varIndex, out var setVarResetSocket, out var setVarResetFlowIn, out var setVarResetFlowOut);
                setVarResetSocket.SetValue(false);
                setVarResetFlowIn.MapToControlInput(once.reset);
            }
            return true;
        }
    }
}
