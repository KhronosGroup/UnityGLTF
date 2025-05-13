using System;
using Unity.VisualScripting;
using UnityEditor;
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

            var setVar = unitExporter.CreateNode<Variable_SetNode>();

            setVar.Configuration["variable"].Value = varIndex;
            setVar.ValueInConnection[Variable_SetNode.IdInputValue].Value = true;
            // Set OnceVariable to true when Branch is false 
            branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).ConnectToFlowDestination(setVar.FlowIn(Variable_SetNode.IdFlowIn));
            // Map once.once out flow to SetVariable outflow
            setVar.FlowOut(Variable_SetNode.IdFlowOut).MapToControlOutput(once.once);
            
            if (once.reset.hasAnyConnection)
            {
                var resetVar = unitExporter.CreateNode<Variable_SetNode>();
                
                resetVar.Configuration["variable"].Value = varIndex;
                resetVar.ValueInConnection[Variable_SetNode.IdInputValue].Value = false;
                resetVar.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(once.reset);
            }
            return true;
        }
    }
}
