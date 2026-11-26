using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SequenceUnitExport : IUnitExporter, ICoroutineAwaiter, ICoroutineWait
    {
        public Type unitType { get => typeof(Sequence); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SequenceUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Sequence;
            var validOutputs = unit.multiOutputs.Where( output => output.connection != null && output.connection.destination != null).ToList();
         
            bool coroutine = CoroutineHelper.CoroutineRequired(unit);
            
            GltfInteractivityExportNode node;
            if (!coroutine)
            {
                node = unitExporter.CreateNode<Flow_SequenceNode>();
                unitExporter.MapInputPortToSocketName(unit.enter, Flow_SequenceNode.IdFlowIn, node);
                if (unit.multiOutputs.Count > 0)
                {
                    int index = 0;
                    foreach (var output in validOutputs)
                    {
                        var id = "sequ" + index.ToString("D3");
                        node.FlowOut(id).MapToControlOutput(output);
                        index++;
                    }
                }
            }
            else
            {
                node = unitExporter.CreateNode<Flow_SequenceNode>();
                node.FlowIn(Flow_SequenceNode.IdFlowIn).MapToControlInput(unit.enter);

                var multiGate = unitExporter.CreateNode<Flow_MultiGateNode>();
                multiGate.Configuration[Flow_MultiGateNode.IdConfigIsLoop].Value = false;
                multiGate.Configuration[Flow_MultiGateNode.IdConfigIsRandom].Value = false;

                node.FlowOut("a").ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowInReset));
                node.FlowOut("b").ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowIn));

                if (unit.multiOutputs.Count > 0)
                {
                    int index = 0;
                    foreach (var output in validOutputs)
                    {
                        // the next output starts when this one and the waits it started are done
                        var awaiterNode = CoroutineHelper.AddBodyAwaiter(unitExporter, multiGate.FlowOut(index.ToString()), output);
                        awaiterNode.FlowOutDoneSocket()
                            .ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowIn));

                        index++;
                    }
                }
                unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
                {
                    var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, multiGate as GltfInteractivityUnitExporterNode);
                    if (awaiter == null)
                        return;

                    var nextIndex = multiGate.FlowConnections.Count;

                    var newFlowOut = multiGate.FlowOut(nextIndex.ToString());
                    awaiter.AddCoroutineWait(unitExporter, node, Flow_SequenceNode.IdFlowIn, multiGate, newFlowOut.socket.Key);
                };

            }

            return true;
        }

    }
}