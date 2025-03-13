using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
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
            
            GltfInteractivityUnitExporterNode node;
            if (!coroutine)
            {
                node = unitExporter.CreateNode(new Flow_SequenceNode());
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
                node = unitExporter.CreateNode(new Flow_SequenceNode());
                node.FlowIn(Flow_SequenceNode.IdFlowIn).MapToControlInput(unit.enter);

                var multiGate = unitExporter.CreateNode(new Flow_MultiGateNode());
                multiGate.ConfigurationData[Flow_MultiGateNode.IdConfigIsLoop].Value = false;
                multiGate.ConfigurationData[Flow_MultiGateNode.IdConfigIsRandom].Value = false;

                node.FlowOut("a").ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowInReset));
                node.FlowOut("b").ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowIn));

                if (unit.multiOutputs.Count > 0)
                {
                    int index = 0;
                    foreach (var output in validOutputs)
                    {
                        multiGate.FlowOut(index.ToString()).MapToControlOutput(output);
                        var awaiter = CoroutineHelper.AddCoroutineAwaiter(unitExporter, index.ToString());
                        awaiter.FlowOutDoneSocket()
                            .ConnectToFlowDestination(multiGate.FlowIn(Flow_MultiGateNode.IdFlowIn));
                        index++;
                    }
                }

                unitExporter.exportContext.OnNodesCreated += (nodes) =>
                {
                    var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, node);
                    if (awaiter == null)
                        return;

                    var nextIndex = multiGate.FlowSocketConnectionData.Count;

                    var newFlowOut = multiGate.FlowOut(nextIndex.ToString());
                    awaiter.AddCoroutineWait(unitExporter, multiGate, newFlowOut.socket.Key);
                };
            }

            return true;
        }

    }
}