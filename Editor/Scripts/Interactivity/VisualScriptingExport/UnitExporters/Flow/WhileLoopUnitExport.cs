using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

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
            var breakVariable = LoopBreakHelper.GetBreakVariable(unitExporter, unit);

            if (!coroutine)
            {
                var node = unitExporter.CreateNode<Flow_WhileNode>();

                if (breakVariable == -1)
                {
                    node.FlowIn(Flow_WhileNode.IdFlowIn).MapToControlInput(unit.enter);
                    unitExporter.MapInputPortToSocketName(unit.condition, Flow_WhileNode.IdCondition, node);
                }
                else
                {
                    // Reset the break variable when the loop starts and stop when a Break sets it
                    LoopBreakHelper.ResetBreak(unitExporter, breakVariable, out var resetIn, out var resetOut);
                    resetIn.MapToControlInput(unit.enter);
                    resetOut.ConnectToFlowDestination(node.FlowIn(Flow_WhileNode.IdFlowIn));
                    node.ValueIn(Flow_WhileNode.IdCondition).ConnectToSource(LoopBreakHelper.AndNotBroken(unitExporter, breakVariable, unit.condition));
                }
                unitExporter.MapOutFlowConnectionWhenValid(unit.body, Flow_WhileNode.IdLoopBody, node);
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_WhileNode.IdCompleted, node);
            }
            else
            {
                var branch  = unitExporter.CreateNode<Flow_BranchNode>();
                if (breakVariable == -1)
                {
                    branch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter);
                    branch.ValueIn(Flow_BranchNode.IdCondition).MapToInputPort(unit.condition);
                }
                else
                {
                    LoopBreakHelper.ResetBreak(unitExporter, breakVariable, out var resetIn, out var resetOut);
                    resetIn.MapToControlInput(unit.enter);
                    resetOut.ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));
                    branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(LoopBreakHelper.AndNotBroken(unitExporter, breakVariable, unit.condition));
                }
                branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.exit);
                branch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.body);
                
                var awaiter = CoroutineHelper.AddCoroutineAwaiter(unitExporter, branch, Flow_BranchNode.IdFlowOutTrue);
                awaiter.FlowOutDoneSocket().ConnectToFlowDestination(branch.FlowIn(Flow_BranchNode.IdFlowIn));
                    
                //Need to add additional nodes after we know which other nodes are created
                unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
                {
                    var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, branch as GltfInteractivityUnitExporterNode);
                    if (awaiter == null)
                        return;
                    awaiter.AddCoroutineWait(unitExporter, branch, Flow_BranchNode.IdFlowOutFalse);
                };
            }

            
            return true;
        }
    }
}