using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class DestinationFlowConnections
    {
        public class NodeToNodeSocketConnection
        {
            public GltfInteractivityUnitExporterNode destination;
            public GltfInteractivityUnitExporterNode source;
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

        public void AddWhenValid(ControlOutput controlOutput, string id, GltfInteractivityUnitExporterNode node)
        {
            AddWhenValid(controlOutput, node.FlowConnections[id]);
        }

        public void AddNodeConnection(GltfInteractivityUnitExporterNode destinationNode, string destinationSocketName,
            GltfInteractivityUnitExporterNode sourceNode, string sourceSocketName)
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
        public GltfInteractivityUnitExporterNode node;

        public NodeSocketName(string socketName, GltfInteractivityUnitExporterNode node)
        {
            this.socketName = socketName;
            this.node = node;
        }
    }

    public class UnitExporter
    {
        public ExportPriority unitExportPriority { get; private set; }
        public IUnitExporter exporter { get; private set; }
        public IUnit unit { get; private set; }
        public bool IsTranslatable = true;
        public VisualScriptingExportContext exportContext { get; private set; }
        public VisualScriptingExportContext.ExportGraph Graph { get; private set; }
        private GameObject scriptMachineGameObject;

        private readonly DestinationFlowConnections outFlowConnections = new DestinationFlowConnections();

        public readonly Dictionary<IUnitInputPort, List<NodeSocketName>> inputPortToSocketNameMapping =
            new Dictionary<IUnitInputPort, List<NodeSocketName>>();

        public readonly Dictionary<IUnitOutputPort, NodeSocketName> outputPortToSocketNameByPort =
            new Dictionary<IUnitOutputPort, NodeSocketName>();
        
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

        public GltfInteractivityUnitExporterNode CreateNode(GltfInteractivityNodeSchema schema)
        {
            var newNode = new GltfInteractivityUnitExporterNode(this, schema);
            AddNode(newNode);
            return newNode;
        }
        
        public void AddCustomNode(GltfInteractivityUnitExporterNode node)
        {
            AddNode(node);
        }

        public UnitExporter(VisualScriptingExportContext exportContext, IUnitExporter exporter, IUnit unit)
        {
            this.exporter = exporter;
            this.unit = unit;
            this.exportContext = exportContext;
            this.Graph = exportContext.currentGraphProcessing;
            this.scriptMachineGameObject = exportContext.ActiveScriptMachine.gameObject;

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
                Console.WriteLine(e);
            }
            
            if (!IsTranslatable)
                UnitExportLogging.AddErrorLog(unit, "Could not be exported to GLTF.");
        }

        public void ConvertValue(object originalValue, out object convertedValue, out int typeIndex)
        {
            if (originalValue is GameObject gameObject)
            {
                var gameObjectNodeIndex =
                    exportContext.exporter.GetTransformIndex(gameObject.transform);

                convertedValue = gameObjectNodeIndex;
                typeIndex = GltfTypes.TypeIndexByGltfSignature("int");
            }
            else if (originalValue is Component component)
            {
                var gameObjectNodeIndex =
                    exportContext.exporter.GetTransformIndex(component.transform);
                convertedValue = gameObjectNodeIndex;
                typeIndex = GltfTypes.TypeIndexByGltfSignature("int");
            }
            else if (originalValue is Material material)
            {
                var materialIndex = exportContext.exporter.ExportMaterial(material).Id;
                convertedValue = materialIndex;
                typeIndex = GltfTypes.TypeIndexByGltfSignature("int");
            }
            else
            {
                typeIndex = GltfTypes.TypeIndex(originalValue.GetType());
                convertedValue = originalValue;
            }            
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
                                ConvertValue(defaultValue, out var convertedValue, out int typeIndex);
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
        
        public string GetFirstInputSocketName(IUnitInputPort input, out GltfInteractivityUnitExporterNode node)
        {
            node = null;
            if (inputPortToSocketNameMapping.TryGetValue(input, out var list) && list.Count > 0)
            {
                node = list[0].node;
                return list[0].socketName;
            }
            
            return null;
        }

        public string GetOutputSocketName(IUnitOutputPort output, out GltfInteractivityUnitExporterNode node)
        {
            node = null;
            if (outputPortToSocketNameByPort.ContainsKey(output))
            {
                node = outputPortToSocketNameByPort[output].node;
                return outputPortToSocketNameByPort[output].socketName;
            }

            return null;
        }

        public void MapOutFlowConnectionWhenValid(ControlOutput destinationPort, string sourceSocketName,
            GltfInteractivityUnitExporterNode sourceNode)
        {
            outFlowConnections.AddWhenValid(destinationPort, sourceSocketName, sourceNode);
        }

        public void MapOutFlowConnection(GltfInteractivityUnitExporterNode destinationNode, string destinationSocketName,
            GltfInteractivityUnitExporterNode sourceNode, string sourceSocketName)
        {
            outFlowConnections.AddNodeConnection(destinationNode, destinationSocketName, sourceNode, sourceSocketName);
        }
        
        private IUnitInputPort ResolveBypass(IUnitInputPort inputPort,
            ref VisualScriptingExportContext.ExportGraph graph)
        {
            if (Graph.bypasses.TryGetValue(inputPort, out var byPassInputPort))
            {
                graph = Graph;
                return ResolveBypass(byPassInputPort, ref graph);
            }

            if (exportContext.graphBypasses.TryGetValue(
                    new VisualScriptingExportContext.InputPortGraph(inputPort, graph), out var graphByPassInputPort))
            {
                inputPort = graphByPassInputPort.port;
                graph = graphByPassInputPort.graph;

                return ResolveBypass(inputPort, ref graph);
            }

            return inputPort;
        }

        public void ResolveConnections()
        {
            void SetInputConnection(UnitExporter exportNode, List<NodeSocketName> toSockets, IUnitOutputPort port)
            {
                var socketName = exportNode.GetOutputSocketName(port, out var sourceNode);
                if (socketName != null)
                {
                    foreach (var socket in toSockets)
                    {
                        socket.node.ValueInConnection[socket.socketName].Node = sourceNode.Index;
                        socket.node.ValueInConnection[socket.socketName].Socket = socketName;
                    }
                }
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
   
                if (inputPort.hasValidConnection)
                {
                    var firstConnection = inputPort.connections.First();

                    if (inputPortGraph.nodes.ContainsKey(firstConnection.source.unit))
                    {
                        var sourcePort = firstConnection.source;
                        SetInputConnection(inputPortGraph.nodes[sourcePort.unit], input.Value, sourcePort);
                    }
                    else
                    {
                        // Search in the graph for the node that has the output port, in case it's kind of a bypass.
                        foreach (var node in Graph.nodes)
                        {
                            if (node.Value.outputPortToSocketNameByPort.ContainsKey(firstConnection.source))
                                SetInputConnection(node.Value, input.Value, firstConnection.source);
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
        }
        
        public bool HasPortMappingTo(ValueOutput valueOutput)
        {
            return outputPortToSocketNameByPort.ContainsKey(valueOutput);
        }
        
        public void MapValueOutportToSocketName(IUnitOutputPort outputPort, string socketName, GltfInteractivityUnitExporterNode node)
        {
            outputPortToSocketNameByPort.Add(outputPort, new NodeSocketName(socketName, node));
        }
        
        public void MapInputPortToSocketName(IUnitInputPort inputPort, string socketName, GltfInteractivityUnitExporterNode node)
        {
            if (!inputPortToSocketNameMapping.TryGetValue(inputPort, out var portList))
            {
                portList = new List<NodeSocketName>();
                inputPortToSocketNameMapping.Add(inputPort, portList);
            }

            portList.Add(new NodeSocketName(socketName, node));
        }

        public bool IsInputLiteralOrDefaultValue(ValueInput inputPort, out object value)
        {
            if (inputPort.hasValidConnection && inputPort.connections.First().source.unit is GraphInput)
            {
                var subGraphUnit = exportContext.currentGraphProcessing.subGraphUnit;
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
            exportContext.graphBypasses.Add(new VisualScriptingExportContext.InputPortGraph(controlInput, inputGraph),
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
                exportContext.graphBypasses.Add(
                    new VisualScriptingExportContext.InputPortGraph(
                        valueOut.destination.connections.First().destination, outputGraph),
                    new VisualScriptingExportContext.InputPortGraph(valueInput, inputGraph));
            }
        }

        public void MapInputPortToSocketName(string sourceSocketName, GltfInteractivityUnitExporterNode sourceNode,
            string socketName, GltfInteractivityUnitExporterNode node)
        {
            nodeInputPortToSocketNameMapping.Add(new NodeSocketName(sourceSocketName, sourceNode),
                new NodeSocketName(socketName, node));
        }
    }
}