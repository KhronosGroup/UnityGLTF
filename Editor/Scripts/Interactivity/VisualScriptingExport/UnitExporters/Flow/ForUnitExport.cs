using System;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ForUnitExport : IUnitExporter, ICoroutineAwaiter, ICoroutineWait
    {
        public Type unitType
        {
            get => typeof(Unity.VisualScripting.For);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ForUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.For;
            
            bool coroutine = CoroutineHelper.CoroutineRequired(unit);
            
            if (!coroutine && unitExporter.IsInputLiteralOrDefaultValue(unit.step, out var defaultStepValue) && (int)defaultStepValue == 1)
            {
                var node = unitExporter.CreateNode<Flow_ForLoopNode>();
                // TODO: set inital index > also... why even using it
                node.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
                
                unitExporter.MapInputPortToSocketName(unit.enter, Flow_ForLoopNode.IdFlowIn, node);
                unitExporter.MapInputPortToSocketName(unit.firstIndex, Flow_ForLoopNode.IdStartIndex, node);
                unitExporter.MapInputPortToSocketName(unit.lastIndex, Flow_ForLoopNode.IdEndIndex, node);

                node.ValueOut(Flow_ForLoopNode.IdIndex).MapToPort(unit.currentIndex).ExpectedType(ExpectedType.Int);
                
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_ForLoopNode.IdCompleted, node);
                unitExporter.MapOutFlowConnectionWhenValid(unit.body, Flow_ForLoopNode.IdLoopBody, node);       
            }
            else
            {
                // possible that Step is not +1, wo we need the custom For Loop
                
                if (!coroutine)
                {
                    FlowHelpersVS.CreateCustomForLoop(unitExporter, out var startIndex,
                        out var endIndex, out var step, 
                        out var flowIn, out var currentIndex, 
                        out var loopBody, out var completed );
                    
                    startIndex.MapToInputPort(unit.firstIndex);
                    endIndex.MapToInputPort(unit.lastIndex);

                    step.MapToInputPort(unit.step);
                    flowIn.MapToControlInput(unit.enter);
                    currentIndex.MapToPort(unit.currentIndex);
                    loopBody.MapToControlOutput(unit.body);
                    completed.MapToControlOutput(unit.exit);
                }
                else
                {
                    FlowHelpersVS.CreateCustomForLoopWithFlowStep(unitExporter, out var startIndex,
                        out var endIndex, out var step, 
                        out var flowIn,  out var nextStep, out var currentIndex, 
                        out var loopBody, out var completed );
                    
                    startIndex.MapToInputPort(unit.firstIndex);
                    endIndex.MapToInputPort(unit.lastIndex);

                    step.MapToInputPort(unit.step);
                    flowIn.MapToControlInput(unit.enter);
                    currentIndex.MapToPort(unit.currentIndex);
                    loopBody.MapToControlOutput(unit.body);
                    completed.MapToControlOutput(unit.exit);
                    
                    var awaiter = CoroutineHelper.AddCoroutineAwaiter(unitExporter, loopBody.node, loopBody.socket.Key);
                    awaiter.FlowOutDoneSocket().ConnectToFlowDestination(nextStep);
                    
                    unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
                    {
                        var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, flowIn.node as GltfInteractivityUnitExporterNode);
                        if (awaiter == null)
                            return;
                        awaiter.AddCoroutineWait(unitExporter, completed.node, completed.socket.Key);
                    };
                }
            }
            return true;
        }
    }
}