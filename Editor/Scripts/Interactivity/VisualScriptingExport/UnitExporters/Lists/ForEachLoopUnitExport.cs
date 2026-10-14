using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ForEachLoopUnitExport : IUnitExporter, ICoroutineAwaiter, ICoroutineWait
    {
        public Type unitType { get => typeof(ForEach); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ForEachLoopUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ForEach;
            
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            // flow/for can't be interrupted, so loops with a Break use the custom loop
            var breakVariable = LoopBreakHelper.GetBreakVariable(unitExporter, unit);

            // a body with waits: the next item only starts when the body's waits are done (like For)
            if (CoroutineHelper.CoroutineRequired(unit))
            {
                FlowHelpersVS.CreateCustomForLoopWithFlowStep(unitExporter, out var startIndex,
                    out var endIndex, out var step,
                    out var flowIn, out var nextStep, out var currentIndex,
                    out var loopBody, out var completed, breakVariable);

                startIndex.SetValue(0);
                step.SetValue(1);
                ListHelpersVS.GetListCount(list, endIndex);
                flowIn.MapToControlInput(unit.enter);
                completed.MapToControlOutput(unit.exit);

                ListHelpersVS.GetItem(unitExporter, list, currentIndex, out var itemValueSocket);
                currentIndex.MapToPort(unit.currentIndex);
                itemValueSocket.MapToPort(unit.currentItem);

                var awaiter = CoroutineHelper.AddBodyAwaiter(unitExporter, loopBody, unit.body);
                awaiter.FlowOutDoneSocket().ConnectToFlowDestination(nextStep);

                unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
                {
                    var outerAwaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, flowIn.node as GltfInteractivityUnitExporterNode);
                    if (outerAwaiter == null)
                        return;
                    outerAwaiter.AddCoroutineWait(unitExporter, flowIn.node, flowIn.socket.Key, completed.node, completed.socket.Key);
                };
                return true;
            }

            if (breakVariable != -1)
            {
                FlowHelpersVS.CreateCustomForLoop(unitExporter, out var startIndex,
                    out var endIndex, out var step,
                    out var flowIn, out var currentIndex,
                    out var loopBody, out var completed, breakVariable);

                startIndex.SetValue(0);
                step.SetValue(1);
                ListHelpersVS.GetListCount(list, endIndex);
                flowIn.MapToControlInput(unit.enter);
                loopBody.MapToControlOutput(unit.body);
                completed.MapToControlOutput(unit.exit);

                ListHelpersVS.GetItem(unitExporter, list, currentIndex, out var itemValueSocket);
                currentIndex.MapToPort(unit.currentIndex);
                itemValueSocket.MapToPort(unit.currentItem);
                return true;
            }

            var forLoop = unitExporter.CreateNode<Flow_ForLoopNode>();
            forLoop.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
            forLoop.ValueIn(Flow_ForLoopNode.IdStartIndex).SetValue(0);

            forLoop.FlowIn(Flow_ForLoopNode.IdFlowIn).MapToControlInput(unit.enter);
            forLoop.FlowOut(Flow_ForLoopNode.IdLoopBody).MapToControlOutput(unit.body);
            forLoop.FlowOut(Flow_ForLoopNode.IdCompleted).MapToControlOutput(unit.exit);
            
            ListHelpersVS.GetListCount(list, forLoop.ValueIn(Flow_ForLoopNode.IdEndIndex));
            
            ListHelpersVS.GetItem(unitExporter, list, forLoop.ValueOut(Flow_ForLoopNode.IdIndex), out var getItemValueSocket);

            forLoop.ValueOut(Flow_ForLoopNode.IdIndex).MapToPort(unit.currentIndex);
            
            getItemValueSocket.MapToPort(unit.currentItem);
            return true;
        }
    }
}