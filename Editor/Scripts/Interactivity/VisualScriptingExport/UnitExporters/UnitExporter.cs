using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class DestinationFlowConnections
    {
        public class NodeToNodeSocketConnection
        {
            public GltfInteractivityExportNode destination;
            public GltfInteractivityExportNode source;
            public string destinationSocketName;
            public string sourceSocketName;
        }

        public Dictionary<ControlOutput, List<GltfInteractivityNode.SocketData>> unitSocketConnections =
            new Dictionary<ControlOutput, List<GltfInteractivityNode.SocketData>>();

        public List<NodeToNodeSocketConnection> nodeSocketConnections = new List<NodeToNodeSocketConnection>();

        public void AddWhenValid(ControlOutput controlOutput, GltfInteractivityNode.SocketData socketData)
        {
            if (controlOutput.hasValidConnection)
            {
                if (!unitSocketConnections.ContainsKey(controlOutput))
                {
                    unitSocketConnections.Add(controlOutput, new List<GltfInteractivityNode.SocketData>());
                }

                unitSocketConnections[controlOutput].Add(socketData);
            }
        }

        public void AddWhenValid(ControlOutput controlOutput, string id, GltfInteractivityExportNode node)
        {
            AddWhenValid(controlOutput, node.FlowConnections[id]);
        }

        public void AddNodeConnection(GltfInteractivityExportNode destinationNode, string destinationSocketName,
            GltfInteractivityExportNode sourceNode, string sourceSocketName)
        {
            nodeSocketConnections.Add(new NodeToNodeSocketConnection()
            {
                destination = destinationNode,
                source = sourceNode,
                destinationSocketName = destinationSocketName,
                sourceSocketName = sourceSocketName
            });
        }
    }

    public class NodeSocketName
    {
        public string socketName;
        public GltfInteractivityExportNode node;

        public NodeSocketName(string socketName, GltfInteractivityExportNode node)
        {
            this.socketName = socketName;
            this.node = node;
        }
    }

    public class UnitExporter : IUnitSocketConnector, INodeExporter
    {
        public ExportPriority unitExportPriority { get; private set; }
        public IUnitExporter exporter { get; private set; }
        public IUnit unit { get; private set; }
        public bool IsTranslatable = true;
        public VisualScriptingExportContext vsExportContext { get; private set; }
        
        public virtual InteractivityExportContext Context { get => vsExportContext; }
        
        public VisualScriptingExportContext.ExportGraph Graph { get; private set; }
        private GameObject scriptMachineGameObject;

        private readonly DestinationFlowConnections outFlowConnections = new DestinationFlowConnections();

        public readonly Dictionary<IUnitInputPort, List<NodeSocketName>> inputPortToSocketNameMapping =
            new Dictionary<IUnitInputPort, List<NodeSocketName>>();

        public readonly Dictionary<IUnitOutputPort, List<NodeSocketName>> outputPortToSocketNameByPort =
            new Dictionary<IUnitOutputPort, List<NodeSocketName>>();
        
        public readonly Dictionary<NodeSocketName, NodeSocketName> nodeInputPortToSocketNameMapping =
            new Dictionary<NodeSocketName, NodeSocketName>();

        private List<GltfInteractivityUnitExporterNode> _nodes = new List<GltfInteractivityUnitExporterNode>();

        public GltfInteractivityUnitExporterNode[] Nodes
        {
            get => _nodes.ToArray();
        }

        private void AddNode(GltfInteractivityUnitExporterNode node)
        {
            _nodes.Add(node);
        }

        public virtual GltfInteractivityExportNode CreateNode<TSchema>() where TSchema : GltfInteractivityNodeSchema, new()
        {
            var newNode = new GltfInteractivityUnitExporterNode(this, GltfInteractivityNodeSchema.GetSchema<TSchema>());
            AddNode(newNode);
            return newNode;
        }
        
        public virtual GltfInteractivityExportNode CreateNode(Type schemaType)
        {
            var newNode = new GltfInteractivityUnitExporterNode(this, GltfInteractivityNodeSchema.GetSchema(schemaType));
            AddNode(newNode);
            return newNode;
        }
        
        public void AddCustomNode(GltfInteractivityUnitExporterNode node)
        {
            AddNode(node);
        }

        public UnitExporter(VisualScriptingExportContext vsExportContext, IUnitExporter exporter, IUnit unit)
        {
            this.exporter = exporter;
            this.unit = unit;
            this.vsExportContext = vsExportContext;
            this.Graph = vsExportContext.currentGraphProcessing;
            this.scriptMachineGameObject = vsExportContext.ActiveScriptMachine.gameObject;

            var unitExportPriorityAttribute = exporter.GetType().GetAttribute<UnitExportPriority>(true);
            if (unitExportPriorityAttribute != null)
            {
                this.unitExportPriority = unitExportPriorityAttribute.priority;
            }
            else
                this.unitExportPriority = ExportPriority.Default;
        }
        
        public void InitializeInteractivityNode()
        {
            try
            {
                IsTranslatable = exporter.InitializeInteractivityNodes(this);
                if (IsTranslatable)
                {
                    // Check for Chaining and Bypass Target Value
                    if (unit is InvokeMember invokeMemberUnit)
                    {
                        if (invokeMemberUnit.supportsChaining && invokeMemberUnit.chainable)
                            if (!HasPortMappingTo(invokeMemberUnit.targetOutput))
                                ByPassValue(invokeMemberUnit.target, invokeMemberUnit.targetOutput);
                    }
                    if (unit is SetMember setMemberUnit)
                    {
                        if (setMemberUnit.supportsChaining && setMemberUnit.chainable)
                            if (!HasPortMappingTo(setMemberUnit.targetOutput))
                            {
                                ByPassValue(setMemberUnit.target, setMemberUnit.targetOutput);
                            }
                    }
                }
            }
            catch (Exception e)
            {
                IsTranslatable = false;
                UnitExportLogging.AddErrorLog(unit, "Error initializing interactivity nodes: " + e.Message);
                Debug.LogError(e.Message+ "\n" + e.StackTrace);
                Console.WriteLine(e);
            }
            
            if (!IsTranslatable)
                UnitExportLogging.AddErrorLog(unit, "Could not be exported to GLTF.");
        }
        
        public void ResolveDefaultAndLiterals()
        {
            foreach (var input in inputPortToSocketNameMapping)
            {
                VisualScriptingExportContext.ExportGraph graph = Graph;
                var resolvedInputPort = ResolveBypass(input.Key, ref graph);

                var valueInputPort = resolvedInputPort as ValueInput;
                if (valueInputPort == null)
                    continue;

                if (IsInputLiteralOrDefaultValue(valueInputPort, out var defaultValue))
                {
                    foreach (var inputPort in input.Value)
                    {
                        if (inputPort.node.ValueInConnection.TryGetValue(inputPort.socketName,
                                out var valueSocketData))
                        {
                            if (valueSocketData.Value == null)
                            {
                                Context.ConvertValue(defaultValue, out var convertedValue, out int typeIndex);
                                valueSocketData.Value = convertedValue;
                                valueSocketData.Type = typeIndex;
                                if (typeIndex == -1 && defaultValue != null)
                                    UnitExportLogging.AddErrorLog(unit, "Unsupported type: " + defaultValue.GetType().ToString());
                            }
                        }
                        else if (inputPort.node.Configuration.TryGetValue(inputPort.socketName,
                                     out var config))
                        {
                            // Is it relevant anymore??
                            // TODO how do we correctly update the config value here?
                            // We don't know what it is – e.g. when its an event, we need to put the index of that event here.
                            // config.Value = literal;
                        }
                        else
                        {
                            throw new System.Exception(
                                "ValueSocketConnectionData nor ConfigurationData  contains key: " + input.Value +
                                ", instead: [Value: " +
                                string.Join(", ", inputPort.node.ValueInConnection.Keys) +
                                "], [Config: " +
                                string.Join(", ", inputPort.node.Configuration.Keys) + "]");
                        }
                    }
                }
            }
        }
        
        public string GetFirstInputSocketName(IUnitInputPort input, out GltfInteractivityExportNode node)
        {
            node = null;
            if (inputPortToSocketNameMapping.TryGetValue(input, out var list) && list.Count > 0)
            {
                node = list[0].node;
                return list[0].socketName;
            }
            
            return null;
        }
        
        public string GetFirstOutputSocketName(IUnitOutputPort output, out GltfInteractivityExportNode node)
        {
            node = null;
            if (outputPortToSocketNameByPort.TryGetValue(output, out var list) && list.Count > 0)
            {
                node = list[0].node;
                return list[0].socketName;
            }
            
            return null;
        }
        
        public List<NodeSocketName> GetOutputSocketNameMap(IUnitOutputPort output)
        {
            if (outputPortToSocketNameByPort.TryGetValue(output, out var list) && list.Count > 0)
            {
                return list;
            }
            
            return null;
        }
        
        private IUnitInputPort ResolveBypass(IUnitInputPort inputPort,
            ref VisualScriptingExportContext.ExportGraph graph)
        {
            if (Graph.bypasses.TryGetValue(inputPort, out var byPassInputPort))
            {
                graph = Graph;
                return ResolveBypass(byPassInputPort, ref graph);
            }

            if (vsExportContext.graphBypasses.TryGetValue(
                    new VisualScriptingExportContext.InputPortGraph(inputPort, graph), out var graphByPassInputPort))
            {
                inputPort = graphByPassInputPort.port;
                graph = graphByPassInputPort.graph;

                return ResolveBypass(inputPort, ref graph);
            }

            return inputPort;
        }

        private void SetAllUnitExporterNodesToDirectSocketConnector()
        {
            // default socket connector for Unit Exporter Nodes is based on mapping.
            // After ResolvingConnections, we can set connection directly, because we now have node indices
            foreach (var node in _nodes)
            {
                node.SocketConnector = new DirectSocketConnector();
            }
        }
        
        public void ResolveConnections()
        {
            bool SetInputConnection(UnitExporter exportNode, List<NodeSocketName> toSockets, IUnitOutputPort port)
            {
                var maps = exportNode.GetOutputSocketNameMap(port);
                if (maps != null)
                {
                    foreach (var m in maps)
                    {
                        foreach (var socket in toSockets)
                        {
                            socket.node.ValueInConnection[socket.socketName].Node = m.node.Index;
                            socket.node.ValueInConnection[socket.socketName].Socket = m.socketName;
                        }
                    }

                    return true;
                }

                return false;

                // var socketName = exportNode.GetFirstOutputSocketName(port, out var sourceNode);
                // if (socketName != null)
                // {
                //     foreach (var socket in toSockets)
                //     {
                //         socket.node.ValueInConnection[socket.socketName].Node = sourceNode.Index;
                //         socket.node.ValueInConnection[socket.socketName].Socket = socketName;
                //     }
                // }
            }

            void SetOutFlowConnection(UnitExporter exportNode, List<GltfInteractivityUnitExporterNode.SocketData> toSockets,
                IUnitInputPort port)
            {
                var socketName = exportNode.GetFirstInputSocketName(port, out var node);
                if (socketName != null)
                {
                    foreach (var socketData in toSockets)
                    {
                        socketData.Socket = socketName;
                        socketData.Node = node.Index;
                    }
                }
            }
            
            // Resolve Input Socket Connections
            foreach (var input in inputPortToSocketNameMapping)
            {
                var inputPortGraph = Graph;
                IUnitInputPort inputPort = input.Key;
                inputPort = ResolveBypass(inputPort, ref inputPortGraph);
                bool resolved = false;
                if (inputPort.hasValidConnection)
                {
                    var firstConnection = inputPort.connections.First();

                    if (inputPortGraph.nodes.ContainsKey(firstConnection.source.unit))
                    {
                        var sourcePort = firstConnection.source;
                        resolved = SetInputConnection(inputPortGraph.nodes[sourcePort.unit], input.Value, sourcePort);
                    }
                    
                    if (!resolved)
                    {
                        // Search in the graph for the node that has the output port, in case it's kind of a bypass.
                        foreach (var node in Graph.nodes)
                        {
                            if (node.Value.outputPortToSocketNameByPort.ContainsKey(firstConnection.source))
                                resolved = SetInputConnection(node.Value, input.Value, firstConnection.source);
                        }

                        if (!resolved)
                        {
                            foreach (var graph in vsExportContext.addedGraphs)
                                foreach (var node in graph.nodes)
                                {
                                    if (node.Value.outputPortToSocketNameByPort.ContainsKey(firstConnection.source))
                                        SetInputConnection(node.Value, input.Value, firstConnection.source);
                                }
                        }
                    }
                }
            }
            
            foreach (var n in nodeInputPortToSocketNameMapping)
            {
                if (!n.Value.node.ValueInConnection.ContainsKey(n.Value.socketName)) 
                    continue;
                n.Value.node.ValueInConnection[n.Value.socketName].Node = n.Key.node.Index;
                n.Value.node.ValueInConnection[n.Value.socketName].Socket = n.Key.socketName;
            }

            // Resolve Out Flow Connections
            foreach (var (inputPort, socketDataList) in outFlowConnections.unitSocketConnections)
            {
                var destinationInputPortGraph = Graph;
                IUnitInputPort destinationInputPort = inputPort.connection.destination;

                destinationInputPort = ResolveBypass(destinationInputPort, ref destinationInputPortGraph);

                IUnit otherUnit = destinationInputPort.unit;
                if (destinationInputPortGraph.nodes.ContainsKey(otherUnit))
                {
                    // Get the index of the other node and the socket name
                    var otherAdapter = destinationInputPortGraph.nodes[otherUnit];
                    SetOutFlowConnection(otherAdapter, socketDataList, destinationInputPort);
                }
                else
                {
                    foreach (var node in Graph.nodes)
                    {
                        if (node.Value.inputPortToSocketNameMapping.ContainsKey(destinationInputPort))
                            SetOutFlowConnection(node.Value, socketDataList, destinationInputPort);
                    }
                }
            }

            foreach (var nodeConnection in outFlowConnections.nodeSocketConnections)
            {
                var destinationNode = nodeConnection.destination;
                var sourceNode = nodeConnection.source;
                var destinationSocketName = nodeConnection.destinationSocketName;
                var sourceSocketName = nodeConnection.sourceSocketName;

                sourceNode.FlowConnections[sourceSocketName].Node = destinationNode.Index;
                sourceNode.FlowConnections[sourceSocketName].Socket = destinationSocketName;
            }
            
            SetAllUnitExporterNodesToDirectSocketConnector();
        }
        
        public bool HasPortMappingTo(ValueOutput valueOutput)
        {
            return outputPortToSocketNameByPort.ContainsKey(valueOutput);
        }
        
        public bool IsInputLiteralOrDefaultValue(ValueInput inputPort, out object value)
        {
            if (inputPort.hasValidConnection && inputPort.connections.First().source.unit is GraphInput)
            {
                var subGraphUnit = vsExportContext.currentGraphProcessing.subGraphUnit;
                if (subGraphUnit != null)
                {
                    var graphValueKey = inputPort.connections.First().source.key;
                    // Reroute to the SubGraph Unit to get the value
                    inputPort = subGraphUnit.valueInputs[graphValueKey];
                }
            }
            
            if (inputPort.hasDefaultValue && !inputPort.hasValidConnection && !inputPort.hasAnyConnection)
            {
                value = unit.defaultValues[inputPort.key];

                if (value == null && (inputPort.type == typeof(GameObject) || inputPort.type.IsSubclassOf(typeof(Component))))
                    // Self reference
                    value = scriptMachineGameObject;

                return true;
            }

            if (inputPort.hasValidConnection && inputPort.connections.First().source.unit is This)
            {
                value = scriptMachineGameObject;
                return true;
            }

            if (inputPort.hasValidConnection && inputPort.connections.First().source.unit is Null)
            {
                value = -1;
                return true;
            }
            
            if (inputPort.hasValidConnection && inputPort.connections.First().source.unit is Literal literal)
            {
                value = literal.value;

                if (value == null && (inputPort.type == typeof(GameObject) ||
                                      inputPort.type.IsSubclassOf(typeof(Component))))
                    // Self reference
                    value = scriptMachineGameObject;

                return true;
            }

            value = null;
            return false;
        }

        // In case a Gltf Node has no flow connections, we can bypass the flow.
        public void ByPassFlow(ControlInput controlInput, ControlOutput controlOutput)
        {
            if (!controlInput.hasValidConnection || !controlOutput.hasValidConnection)
                return;

            var outFlow = controlOutput.validConnections.First();
            Graph.bypasses.Add(controlInput, outFlow.destination);
        }

        public void ByPassFlow(ControlInput controlInput, VisualScriptingExportContext.ExportGraph inputGraph,
            ControlOutput controlOutput, VisualScriptingExportContext.ExportGraph outputGraph)
        {
            if (!controlInput.hasValidConnection || !controlOutput.hasValidConnection)
                return;

            var outFlow = controlOutput.validConnections.First();
            vsExportContext.graphBypasses.Add(new VisualScriptingExportContext.InputPortGraph(controlInput, inputGraph),
                new VisualScriptingExportContext.InputPortGraph(outFlow.destination, outputGraph));
        }

        public void ByPassValue(ValueInput valueInput, ValueOutput valueOutput)
        {
            if (!valueInput.hasValidConnection || !valueOutput.hasValidConnection)
                return;

            foreach (var valueOut in valueOutput.validConnections)
                Graph.bypasses.Add(valueOut.destination.connections.First().destination, valueInput);
        }

        public void ByPassValue(ValueInput valueInput, VisualScriptingExportContext.ExportGraph inputGraph,
            ValueOutput valueOutput, VisualScriptingExportContext.ExportGraph outputGraph)
        {
            if (!valueInput.hasValidConnection || !valueOutput.hasValidConnection)
                return;

            foreach (var valueOut in valueOutput.validConnections)
            {
                vsExportContext.graphBypasses.Add(
                    new VisualScriptingExportContext.InputPortGraph(
                        valueOut.destination.connections.First().destination, outputGraph),
                    new VisualScriptingExportContext.InputPortGraph(valueInput, inputGraph));
            }
        }


        public void ConnectOutFlow(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode,
            string fromSocketId)
        {
             outFlowConnections.AddNodeConnection(otherNode, otherSocketId, fromNode, fromSocketId);        
        }

        public void ConnectValueIn(GltfInteractivityExportNode otherNode, string otherSocketId, GltfInteractivityExportNode fromNode,
            string fromSocketId)
        {
            nodeInputPortToSocketNameMapping.Add(new NodeSocketName(otherSocketId, otherNode), new NodeSocketName(fromSocketId, fromNode));         
       }

        public void MapValueOutportToSocketName(IUnitOutputPort outputPort, string socketId, GltfInteractivityExportNode node)
        {
            if (!outputPortToSocketNameByPort.TryGetValue(outputPort, out var portList))
            {
                portList = new List<NodeSocketName>();
                outputPortToSocketNameByPort.Add(outputPort, portList);
            }

            portList.Add(new NodeSocketName(socketId, node));
        }

        public void MapInputPortToSocketName(IUnitInputPort valueInput, string socketId, GltfInteractivityExportNode node)
        {
            if (!inputPortToSocketNameMapping.TryGetValue(valueInput, out var portList))
            {
                portList = new List<NodeSocketName>();
                inputPortToSocketNameMapping.Add(valueInput, portList);
            }

            portList.Add(new NodeSocketName(socketId, node));
        }

        public void MapOutFlowConnectionWhenValid(ControlOutput output, string socketId, GltfInteractivityExportNode node)
        {
            outFlowConnections.AddWhenValid(output, socketId, node);
        }

    }
}