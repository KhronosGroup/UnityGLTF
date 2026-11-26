using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class CoroutineHelper
    {
        public static CoroutineAwaiterNode AddCoroutineAwaiter(UnitExporter unitExporter, GltfInteractivityNode node,  string outFlowSocket)
        {
            return new CoroutineAwaiterNode(unitExporter, node, outFlowSocket);
        }

        /// <summary>
        /// Runs <paramref name="body"/> from <paramref name="bodyOut"/> and waits for it: begin, body, body done. Connect
        /// <see cref="CoroutineAwaiterNode.FlowOutDoneSocket"/> to what follows the body (next output, next loop step).
        /// </summary>
        public static CoroutineAwaiterNode AddBodyAwaiter(UnitExporter unitExporter, FlowOutRef bodyOut, ControlOutput body)
        {
            var bodySequence = unitExporter.CreateNode<Flow_SequenceNode>();
            bodyOut.ConnectToFlowDestination(bodySequence.FlowIn(Flow_SequenceNode.IdFlowIn));

            var awaiter = AddCoroutineAwaiter(unitExporter, bodySequence, "1");
            bodySequence.FlowOut("0").ConnectToFlowDestination(awaiter.BeginIn);
            bodySequence.FlowOut("1").MapToControlOutput(body);
            bodySequence.FlowOut("2").ConnectToFlowDestination(awaiter.BodyDoneIn);
            return awaiter;
        }

        public static bool CoroutineRequired(Unit unit)
        {
            var  visited = new HashSet<IUnit>();
            visited.Add(unit);

            var queue = new Queue<IUnit>();
            queue.Enqueue(unit);

            while (queue.Count > 0)
            {
                var currentUnit = queue.Dequeue();

                if (currentUnit.controlInputs.Any(input => input.requiresCoroutine))
                    return true;

                foreach (var output in currentUnit.controlOutputs.Where( c => c.hasValidConnection))
                {
                    if (!visited.Contains(output.connection.destination.unit))
                    {
                        visited.Add(output.connection.destination.unit);
                        queue.Enqueue(output.connection.destination.unit);
                    }
                }
            }

            return false;
        }

        public static CoroutineAwaiterNode FindCoroutineAwaiter(UnitExporter unitExporter, GltfInteractivityUnitExporterNode coroutineNode)
        {
            var nodes = unitExporter.vsExportContext.Nodes;
            // flows out of an awaiter (its done flow) don't belong to the body it waits for
            var awaiterNodes = new HashSet<GltfInteractivityNode>(nodes.OfType<CoroutineAwaiterNode>().SelectMany(a => a.InternalNodes));

            Queue<GltfInteractivityNode> queue = new Queue<GltfInteractivityNode>();
            var visited = new HashSet<int>();

            queue.Enqueue(coroutineNode);
            visited.Add(coroutineNode.Index);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                foreach (var node in nodes)
                {
                    if (awaiterNodes.Contains(node))
                        continue;

                    foreach (var flow in node.FlowConnections)
                    {
                        if (flow.Value == null || flow.Value.Node == null)
                            continue;

                        if (flow.Value.Node == currentNode.Index && flow.Value.Socket == "in")
                        {
                            if (coroutineNode != node && node is GltfInteractivityUnitExporterNode exporterNode)
                            {
                                if (exporterNode.Exporter != coroutineNode.Exporter)
                                if (exporterNode.Exporter.exporter is ICoroutineAwaiter coroutineAwaiter)
                                {
                                    var awaiter = CoroutineAwaiterNode.GetAwaiterNode(exporterNode.Exporter, node, flow.Key);
                                    if (awaiter != null)
                                        return awaiter;
                                }
                            }

                            if (!visited.Contains(node.Index))
                            {
                                visited.Add(node.Index);
                                queue.Enqueue(node);
                            }

                            break;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Waits for a body (a Sequence output, a loop body) and the waits started in it, then activates
        /// <see cref="FlowOutDoneSocket"/>. Counts at runtime: each wait adds 1 to "pending" when it starts and subtracts 1
        /// when it ends, so waits in branches that don't run aren't waited for. Done once pending is 0 and the body's
        /// synchronous part has finished. The node itself is the check (branch).
        /// </summary>
        public class CoroutineAwaiterNode : GltfInteractivityUnitExporterNode
        {
            public string OutFlowSocketTarget = "";
            public GltfInteractivityNode OutFlowNode;

            /// <summary>All nodes of this awaiter, <see cref="FindCoroutineAwaiter"/> doesn't search through them.</summary>
            public readonly HashSet<GltfInteractivityNode> InternalNodes = new HashSet<GltfInteractivityNode>();

            /// <summary>Before the body: resets the counter.</summary>
            public FlowInRef BeginIn { get; }
            /// <summary>After the body's synchronous part.</summary>
            public FlowInRef BodyDoneIn { get; }

            private readonly FlowInRef incrementIn, decrementIn;
            private readonly FlowOutRef doneOut;

            public static CoroutineAwaiterNode GetAwaiterNode(UnitExporter unitExporter, GltfInteractivityNode fromNode, string outFlowSocket)
            {
                foreach (var node in unitExporter.Nodes)
                        if (node is CoroutineAwaiterNode awaiterNode && awaiterNode.OutFlowSocketTarget == outFlowSocket && awaiterNode.OutFlowNode == fromNode)
                        return awaiterNode;
                return null;
            }

            public CoroutineAwaiterNode(UnitExporter exporter, GltfInteractivityNode node, string outFlowSocketTarget) : base(exporter, new Flow_BranchNode())
            {
                this.OutFlowNode = node;
                this.OutFlowSocketTarget = outFlowSocketTarget;
                exporter.AddCustomNode(this);
                var firstNode = exporter.Nodes.Length;

                var id = System.Guid.NewGuid().ToString();
                var pending = exporter.vsExportContext.AddVariableWithIdIfNeeded("coroutinePending_" + id, 0, GltfTypes.Int);
                var bodyDone = exporter.vsExportContext.AddVariableWithIdIfNeeded("coroutineBodyDone_" + id, false, GltfTypes.Bool);
                var intMath = MathExpression.OfType(exporter, GltfTypes.TypeIndexByGltfSignature(GltfTypes.Int));

                // begin: pending = 0, bodyDone = false
                VariablesHelpers.SetVariableStaticValue(exporter, pending, 0, out var beginIn, out var resetPendingOut);
                VariablesHelpers.SetVariableStaticValue(exporter, bodyDone, false, out var resetBodyDoneIn, out _);
                resetPendingOut.ConnectToFlowDestination(resetBodyDoneIn);
                BeginIn = beginIn;

                // body done: bodyDone = true, check
                VariablesHelpers.SetVariableStaticValue(exporter, bodyDone, true, out var bodyDoneIn, out var bodyDoneOut);
                bodyDoneOut.ConnectToFlowDestination(FlowIn(Flow_BranchNode.IdFlowIn));
                BodyDoneIn = bodyDoneIn;

                // a wait starts: pending += 1
                VariablesHelpers.GetVariable(exporter, pending, out var pendingBeforeIncrement);
                VariablesHelpers.SetVariable(exporter, pending, out var incremented, out incrementIn, out _);
                incremented.ConnectToSource(intMath.Add(pendingBeforeIncrement, 1));

                // a wait ends: pending -= 1, check
                VariablesHelpers.GetVariable(exporter, pending, out var pendingBeforeDecrement);
                VariablesHelpers.SetVariable(exporter, pending, out var decremented, out decrementIn, out var decrementOut);
                decremented.ConnectToSource(intMath.Sub(pendingBeforeDecrement, 1));
                decrementOut.ConnectToFlowDestination(FlowIn(Flow_BranchNode.IdFlowIn));

                // check: pending == 0 and bodyDone, then done (once: bodyDone = false)
                VariablesHelpers.GetVariable(exporter, pending, out var pendingNow);
                VariablesHelpers.GetVariable(exporter, bodyDone, out var bodyDoneNow);
                ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(intMath.And(intMath.Compare<Math_EqNode>(pendingNow, 0), bodyDoneNow));
                VariablesHelpers.SetVariableStaticValue(exporter, bodyDone, false, out var consumeIn, out doneOut);
                FlowOut(Flow_BranchNode.IdFlowOutTrue).ConnectToFlowDestination(consumeIn);

                InternalNodes.Add(this);
                foreach (var created in exporter.Nodes.Skip(firstNode))
                    InternalNodes.Add(created);
            }

            public FlowOutRef FlowOutDoneSocket()
            {
                return doneOut;
            }

            /// <summary>
            /// Waits for a wait (or a unit that waits, like a Sequence or loop) in the body, which starts with the flow into
            /// <paramref name="startNode"/>.<paramref name="startSocket"/> and ends with <paramref name="doneNode"/>.<paramref name="doneSocket"/>.
            /// Called after all nodes were created and connected (OnUnitNodesCreated).
            /// </summary>
            public void AddCoroutineWait(UnitExporter unitExporter, GltfInteractivityNode startNode, string startSocket,
                GltfInteractivityNode doneNode, string doneSocket)
            {
                if (unitExporter.exporter is not ICoroutineWait)
                {
                    UnityEngine.Debug.LogError("Exporter does not implement ICoroutineWait. Cannot add coroutine wait.");
                    return;
                }

                var nodes = unitExporter.vsExportContext.Nodes;

                // start: pending += 1, then the wait. Only flows from outside of the waiting unit: a While enters its own start again.
                var startSequence = AddLateSequence(unitExporter);
                foreach (var node in nodes)
                {
                    if (node == startSequence || (node is GltfInteractivityUnitExporterNode exporterNode && exporterNode.Exporter == unitExporter))
                        continue;
                    foreach (var flow in node.FlowConnections.Values)
                    {
                        if (flow != null && flow.Node == startNode.Index && flow.Socket == startSocket)
                        {
                            flow.Node = startSequence.Index;
                            flow.Socket = Flow_SequenceNode.IdFlowIn;
                        }
                    }
                }
                startSequence.FlowConnections.Add("a", new FlowSocketData { Node = incrementIn.node.Index, Socket = incrementIn.socket.Key });
                startSequence.FlowConnections.Add("b", new FlowSocketData { Node = startNode.Index, Socket = startSocket });

                // done: what follows the wait, then pending -= 1 and the check
                var decrement = new FlowSocketData { Node = decrementIn.node.Index, Socket = decrementIn.socket.Key };
                if (!doneNode.FlowConnections.TryGetValue(doneSocket, out var target) || target == null || target.Node == null || target.Node == -1)
                {
                    doneNode.FlowConnections[doneSocket] = decrement;
                    return;
                }

                var doneSequence = AddLateSequence(unitExporter);
                doneSequence.FlowConnections.Add("a", new FlowSocketData { Node = target.Node, Socket = target.Socket });
                doneSequence.FlowConnections.Add("b", decrement);
                target.Node = doneSequence.Index;
                target.Socket = Flow_SequenceNode.IdFlowIn;
            }

            /// <summary>Sequence node added after the nodes got their indices.</summary>
            private static GltfInteractivityExportNode AddLateSequence(UnitExporter unitExporter)
            {
                var sequence = unitExporter.CreateNode<Flow_SequenceNode>();
                var nodes = unitExporter.vsExportContext.Nodes;
                nodes.Add(sequence);
                sequence.Index = nodes.Count - 1;
                return sequence;
            }
        }
    }
}
