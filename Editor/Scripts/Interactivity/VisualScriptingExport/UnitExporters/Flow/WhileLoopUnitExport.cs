using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WhileLoopUnitExport : IUnitExporter, ICoroutineAwaiter, ICoroutineWait
    {
        public Type unitType
        {
            get => typeof(While);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WhileLoopUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as While;
            
            var coroutine = CoroutineHelper.CoroutineRequired(unit);

            if (!coroutine)
            {
                var node = unitExporter.CreateNode(new Flow_WhileNode());

                unitExporter.MapInputPortToSocketName(unit.condition, Flow_WhileNode.IdCondition, node);
                unitExporter.MapOutFlowConnectionWhenValid(unit.body, Flow_WhileNode.IdLoopBody, node);
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_WhileNode.IdCompleted, node);
            }
            else
            {
                var branch  = unitExporter.CreateNode(new Flow_BranchNode());
                branch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter); 
                branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.exit);
                branch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.body);
                branch.ValueIn(Flow_BranchNode.IdCondition).MapToInputPort(unit.condition);
                
                var awaiter = CoroutineHelper.AddCoroutineAwaiter(unitExporter, Flow_BranchNode.IdFlowOutTrue);
                awaiter.FlowOutDoneSocket().ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));
                    
                unitExporter.exportContext.OnNodesCreated += (nodes) =>
                {
                    var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, branch);
                    if (awaiter == null)
                        return;
                    awaiter.AddCoroutineWait(unitExporter, branch, Flow_BranchNode.IdFlowOutFalse);
                };
            }

            
            return true;
        }
    }
}