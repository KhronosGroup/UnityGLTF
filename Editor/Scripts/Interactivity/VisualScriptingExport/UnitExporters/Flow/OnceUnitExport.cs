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

            var getVar = unitExporter.CreateNode(new Variable_GetNode());

            var varIndex = unitExporter.exportContext.AddVariableWithIdIfNeeded(onceVarName, false, VariableKind.Flow, typeof(bool));
            getVar.Configuration["variable"].Value = varIndex;
            
            var branch = unitExporter.CreateNode(new Flow_BranchNode());
            // Branch flow in - from Once.Enter
            unitExporter.MapInputPortToSocketName(once.enter, Flow_BranchNode.IdFlowIn, branch);
            //Condition - from GetVariable
            unitExporter.MapInputPortToSocketName(Variable_GetNode.IdOutputValue, getVar, Flow_BranchNode.IdCondition, branch);
            
            // Once.After flow to Branch when true
            unitExporter.MapOutFlowConnectionWhenValid(once.after, Flow_BranchNode.IdFlowOutTrue, branch);

            var setVar = unitExporter.CreateNode(new Variable_SetNode());

            setVar.Configuration["variable"].Value = varIndex;
            setVar.ValueInConnection[Variable_SetNode.IdInputValue].Value = true;
            // Set OnceVariable to true when Branch is false 
            unitExporter.MapOutFlowConnection(setVar, Variable_SetNode.IdFlowIn, branch, Flow_BranchNode.IdFlowOutFalse);
            // Map once.once out flow to SetVariable outflow
            unitExporter.MapOutFlowConnectionWhenValid(once.once,  Variable_SetNode.IdFlowOut, setVar);
            
            if (once.reset.hasAnyConnection)
            {
                var resetVar = unitExporter.CreateNode(new Variable_SetNode());
                
                resetVar.Configuration["variable"].Value = varIndex;
                resetVar.ValueInConnection[Variable_SetNode.IdInputValue].Value = false;
                unitExporter.MapInputPortToSocketName(once.reset, Variable_SetNode.IdFlowIn, resetVar);
            }
            return true;
        }
    }
}
