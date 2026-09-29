using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.Export
{
    /// <summary>
    /// Basic functionality for exporting Interactivity Graphs
    /// </summary>
    public class InteractivityExportContext : GLTFExportPluginContext
    {
        public GLTFRoot ActiveGltfRoot = null;
        public GLTFSceneExporter exporter { get; protected set; }
    
        public List<GltfInteractivityGraph.Variable> variables = new List<GltfInteractivityGraph.Variable>();
        public List<GltfInteractivityGraph.CustomEvent> customEvents = new List<GltfInteractivityGraph.CustomEvent>();
        public List<GltfInteractivityGraph.Declaration> opDeclarations = new List<GltfInteractivityGraph.Declaration>();

        protected List<GltfInteractivityExportNode> nodesToSerialize = new List<GltfInteractivityExportNode>();
        public List<GltfInteractivityExportNode> Nodes
        {
            get => nodesToSerialize;
        }
        
        public bool addUnityGltfSpaceConversion = true;
        
        public delegate void OnBeforeSerializationDelegate(List<GltfInteractivityExportNode> nodes);
        public event OnBeforeSerializationDelegate OnBeforeSerialization;
        
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            this.exporter = exporter;
            ActiveGltfRoot = gltfRoot;
            
            TriggerInterfaceExportCallbacks();
            
            AddSelectabilityExtensionToInvisibleNodes();
            // For Value Conversion, we need to presort the nodes, otherwise we might get wrong results
            TopologicalSort();
            CheckForImplicitValueConversions();
            
            CheckForCircularFlows();
            
            CleanUp();
            
            ReplaceSpecialValuesWithNodes();
            // Final Topological Sort
            TopologicalSort(includeFlows: true);
            
            ResolveRefToStaticPointer();
            
            CollectOpDeclarations();

            TriggerOnBeforeSerialization();
            
            ApplyInteractivityExtension();
        }

        /// <summary>
        /// Replace float.positiveInfinite, NaN etc with nodes that can be serialized to JSON. This is needed because JSON does not support these values.
        /// </summary>
        protected virtual void ReplaceSpecialValuesWithNodes()
        {
            var nodes = nodesToSerialize.ToArray();
            
            void ReplaceInputWithNode(GltfInteractivityNode.ValueSocketData socket, params GltfInteractivityNodeSchema[] schema)
            {
                var lastSocket = socket;
                foreach (var s in schema)
                {
                    var node = new GltfInteractivityExportNode(s);
                    node.Index = nodesToSerialize.Count;
                    nodesToSerialize.Add(node);
                    
                    lastSocket.Node = node.Index;
                    lastSocket.Socket = "value";
                    lastSocket.Value = null;

                    if (node.ValueInConnection.ContainsKey("a"))
                        lastSocket = node.ValueInConnection["a"];
                }
                
            }
            
            foreach (var v in nodes)
            {
                foreach (var input in v.ValueInConnection)
                    if (input.Value.Value != null && input.Value.Node == null)
                    {
                        double? special = input.Value.Value is float f ? f : input.Value.Value is double d ? d : (double?)null;
                        if (special.HasValue)
                        {
                            if (double.IsNaN(special.Value))
                                ReplaceInputWithNode(input.Value, new Math_NaNNode());
                            if (double.IsPositiveInfinity(special.Value))
                                ReplaceInputWithNode(input.Value, new Math_InfNode());
                            if (double.IsNegativeInfinity(special.Value))
                            {
                                ReplaceInputWithNode(input.Value, new Math_NegNode(), new Math_InfNode());
                            }
                        }
                    }
            }
        }
        
        protected virtual void ResolveRefToStaticPointer()
        {
            int refTypeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            foreach (var v in variables)
            {
                if (v.Value == null)
                    continue;

                var valueTypeIndex = GltfTypes.TypeIndex(v.Value.GetType());
                if (v.Type == refTypeIndex || valueTypeIndex == refTypeIndex)
                {
                    if (v.Value.GetType() != typeof(string))
                    {
                        if (RefResolver.TryRefToStaticJson(exporter, v.Value, out var jsonPointer))
                        {
                            v.Value = jsonPointer;
                            v.Type = refTypeIndex;
                        }
                        else
                        { 
                            // Reference not found in export context
                            v.Value = null;
                            v.Type = refTypeIndex;
                        }
                    }
                }
            }

            foreach (var n in nodesToSerialize)
            {
                foreach (var vIn in n.ValueInConnection)
                {
                    if (vIn.Value.Value != null && vIn.Value.Type == refTypeIndex &&
                        vIn.Value.Value.GetType() != typeof(string))
                    {
                        if (RefResolver.TryRefToStaticJson(exporter, vIn.Value.Value, out var jsonPointer))
                        {
                            vIn.Value.Value = jsonPointer;
                            vIn.Value.Type = refTypeIndex;
                        }
                        else
                        { 
                            // Reference not found in export context
                            vIn.Value.Value = null;
                            vIn.Value.Type = refTypeIndex;
                        }
                    }
                }
            }

            foreach (var cEvent in customEvents)
            {
                foreach (var values in cEvent.Values)
                {
                    if (values.Value == null || values.Value.Value == null)
                        continue;

                    var value = values.Value.Value;
                    var mapping = GltfTypes.GetTypeMapping(value.GetType());
                    var isRefType = values.Value.Type == GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
                    if (isRefType && mapping == null)
                    {
                        //Debug.LogError("Trying to resolve a reference to static json, but the type is not supported: " + value.GetType().Name);
                        continue;
                    }

                    if (mapping == null)
                    {
                        continue;
                    }
                    if (isRefType || mapping.GltfSignature == GltfTypes.Ref)
                    {
                        if (RefResolver.TryRefToStaticJson(exporter, value, out var jsonPointer))
                        {
                            values.Value.Value = jsonPointer;
                            values.Value.Type = refTypeIndex;
                        }
                        else
                        { 
                            // Reference not found in export context
                            values.Value.Value = null;
                            values.Value.Type = refTypeIndex;
                        }

                    }
                }
            }
        }
        
        protected virtual void ApplyInteractivityExtension()
        {
            // TODO: Add support for multiple graphs and/or check if a graph already exists
            Validator.ValidateData(this);
            
            GltfInteractivityExtension extension = new GltfInteractivityExtension();
            GltfInteractivityGraph mainGraph = new GltfInteractivityGraph();
            extension.graphs = new GltfInteractivityGraph[] {mainGraph};
            mainGraph.Nodes = nodesToSerialize.ToArray();
            mainGraph.Types = CollectAndFilterUsedTypes();
            
            
            mainGraph.Variables = variables.ToArray();
            mainGraph.CustomEvents = customEvents.ToArray();
            mainGraph.Declarations = opDeclarations.ToArray();

            Validator.ValidateSpecification(extension);

            ActiveGltfRoot.AddExtension(GltfInteractivityExtension.ExtensionName, extension);
            
            exporter.DeclareExtensionUsage(GltfInteractivityExtension.ExtensionName);
        }
        
        protected void TriggerOnBeforeSerialization()
        {
            OnBeforeSerialization?.Invoke(nodesToSerialize);
            OnBeforeSerialization = null;
        }

        public void ConvertValue(object originalValue, out object convertedValue, out int typeIndex)
        {
            convertedValue = originalValue;
            if (originalValue is Transform transform)
            {
                typeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            }
            else if (originalValue is GameObject gameObject)
            {
                typeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            }
            else if (originalValue is Component component)
            {
                typeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            }
            else if (originalValue is Material material)
            {
                typeIndex = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref);
            }
            else
            {
                typeIndex = GltfTypes.TypeIndex(originalValue.GetType());
                convertedValue = originalValue;
            }            
        }
        
        public int AddVariableWithIdIfNeeded(string id, System.Type type)
        {
            return AddVariableWithIdIfNeeded(id, System.Activator.CreateInstance(type), GltfTypes.GetTypeMapping(type)?.GltfSignature);
        }
        
        public int AddVariableWithIdIfNeeded(string id, object defaultValue, System.Type type)
        {
            return AddVariableWithIdIfNeeded(id, defaultValue, GltfTypes.GetTypeMapping(type)?.GltfSignature);
        }
        
        public int AddVariableWithIdIfNeeded(string id, object defaultValue, string gltfType)
        {
            return AddVariableWithIdIfNeeded(id, defaultValue, GltfTypes.TypeIndexByGltfSignature(gltfType));
        }

        public int AddVariableWithIdIfNeeded(string id, object defaultValue, int gltfTypeIndex)
        {
            if (gltfTypeIndex == -1)
            {
                if (defaultValue != null)
                    Debug.LogError("Type not supported for variable: " + defaultValue.GetType().Name);
                return -1;
            }
            
            var index = variables.FindIndex(v => v.Name == id);
            if (index != -1)
                return index;

            GltfInteractivityGraph.Variable newVariable = new GltfInteractivityGraph.Variable();
            newVariable.Name = id;
            
            newVariable.Type = gltfTypeIndex;
            
            newVariable.Value = defaultValue;
            variables.Add(newVariable);
            return variables.Count - 1;
        }
        
        public int AddEventWithIdIfNeeded(string id, Dictionary<string, GltfInteractivityNode.EventValues> arguments = null)
        {
            var index = customEvents.FindIndex(customEvents => customEvents.Id == id);
            if (index != -1)
            {
                // Compare the arguments
                return index;
            }

            GltfInteractivityGraph.CustomEvent newEvent = new GltfInteractivityGraph.CustomEvent();
            newEvent.Id = id;
            newEvent.Name = id;

            if (arguments != null)
                newEvent.Values = arguments;

            customEvents.Add(newEvent);
            return customEvents.Count - 1;
        }
        
        public void AddHoverabilityExtensionToNode(int nodeIndex)
        {
            if (nodeIndex == -1)
                return;
            
            var nodeExtensions = ActiveGltfRoot.Nodes[nodeIndex].Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                ActiveGltfRoot.Nodes[nodeIndex].Extensions = nodeExtensions;
            }
            if (!nodeExtensions.ContainsKey(KHR_node_hoverability_Factory.EXTENSION_NAME))
            {
                nodeExtensions.Add(KHR_node_hoverability_Factory.EXTENSION_NAME, new KHR_node_hoverability());
            }
            exporter.DeclareExtensionUsage(KHR_node_hoverability_Factory.EXTENSION_NAME, false);
        }

        public void AddVisibilityExtensionToNode(int nodeIndex)
        {
            if (nodeIndex == -1)
                return;
            
            var nodeExtensions = ActiveGltfRoot.Nodes[nodeIndex].Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                ActiveGltfRoot.Nodes[nodeIndex].Extensions = nodeExtensions;
            }
            if (!nodeExtensions.ContainsKey(KHR_node_visibility_Factory.EXTENSION_NAME))
            {
                nodeExtensions.Add(KHR_node_visibility_Factory.EXTENSION_NAME, new KHR_node_visibility());
            }
            exporter.DeclareExtensionUsage(KHR_node_visibility_Factory.EXTENSION_NAME, false);
        }
        
        public void AddVisibilityExtensionToAllNodes()
        {
            foreach (var node in ActiveGltfRoot.Nodes)
            {
                var nodeExtensions = node.Extensions;
                if (nodeExtensions == null)
                {
                    nodeExtensions = new Dictionary<string, IExtension>();
                    node.Extensions = nodeExtensions;
                }

                if (!nodeExtensions.ContainsKey(KHR_node_visibility_Factory.EXTENSION_NAME))
                {
                    nodeExtensions.Add(KHR_node_visibility_Factory.EXTENSION_NAME, new KHR_node_visibility());
                }
            }

            exporter.DeclareExtensionUsage(KHR_node_visibility_Factory.EXTENSION_NAME, false);
        }

        public void AddSelectabilityExtensionToNode(int nodeIndex, bool initValue = true)
        {
            if (nodeIndex == -1)
                return;
            
            AddSelectabilityExtensionToNode(ActiveGltfRoot.Nodes[nodeIndex], initValue);
        }

        
        public void AddSelectabilityExtensionToNode(Node node, bool initValue = true)
        {
            if (node == null)
                return;

            var nodeExtensions = node.Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                node.Extensions = nodeExtensions;
            }
            if (!nodeExtensions.TryGetValue(KHR_node_selectability_Factory.EXTENSION_NAME, out var nodeExtension))
            {
                nodeExtensions.Add(KHR_node_selectability_Factory.EXTENSION_NAME, new KHR_node_selectability() { selectable = initValue });
            }
            else
                (nodeExtension as KHR_node_selectability).selectable = initValue;
            
            exporter.DeclareExtensionUsage(KHR_node_selectability_Factory.EXTENSION_NAME, false);
        }
        
        public void AddSelectabilityExtensionToAllNodes()
        {
            foreach (var node in ActiveGltfRoot.Nodes)
            {
                var nodeExtensions = node.Extensions;
                if (nodeExtensions == null)
                {
                    nodeExtensions = new Dictionary<string, IExtension>();
                    node.Extensions = nodeExtensions;
                }

                if (!nodeExtensions.ContainsKey(KHR_node_selectability_Factory.EXTENSION_NAME))
                {
                    nodeExtensions.Add(KHR_node_selectability_Factory.EXTENSION_NAME, new KHR_node_selectability());
                }
            }

            exporter.DeclareExtensionUsage(KHR_node_selectability_Factory.EXTENSION_NAME, false);
        }
        
        protected virtual void TriggerInterfaceExportCallbacks()
        {
            GltfInteractivityExportNodes nodesExport = new GltfInteractivityExportNodes(this);
            
            foreach (var root in exporter.RootTransforms)
            {
                var interfaces = root.GetComponentsInChildren<IInteractivityExport>(true);
                foreach (var callback in interfaces)
                {
                    callback.OnInteractivityExport(nodesExport);
                }                
            }
        }
        
        /// <param name="includeFlows">
        /// false: only value connections are considered (source before consumer), e.g. to resolve types.
        /// true: final order required by the specification, where value inputs must reference nodes with a lower index
        /// AND output flows must point to nodes with a higher index. Remaining cycles over value and flow
        /// connections are broken by routing a flow through a custom event.
        /// </param>
        protected void TopologicalSort(bool includeFlows = false)
        {
            // Resort the nodes after resolving the connections
            var sorted = includeFlows ? SortByValuesAndFlows() : PostTopologicalSort();
            var newIndices = new Dictionary<int, int>(); // Key = Old Index, Value = New Index
            
            int newIndex = 0;
            foreach (var oldIndex in sorted)
            {
                newIndices[oldIndex] = newIndex;
                newIndex++;
            }
            
            // Replace old indices with new 
            foreach (var kvp in newIndices)
            {
                var node = nodesToSerialize[kvp.Key];
                node.Index = kvp.Value;
                
                foreach (var valueSocket in node.ValueInConnection)
                {
                    if (valueSocket.Value.Node != null && valueSocket.Value.Node.HasValue)
                        valueSocket.Value.Node = newIndices[valueSocket.Value.Node.Value];
                }
                foreach (var flowSocket in node.FlowConnections)
                {
                    if (flowSocket.Value.Node != null && flowSocket.Value.Node.HasValue)
                        flowSocket.Value.Node = newIndices[flowSocket.Value.Node.Value];
                }
            }

            // Resort nodesToSerialize
            nodesToSerialize.Sort((a, b) => a.Index.CompareTo(b.Index));
        }
        
        private struct SortEdge
        {
            public int From;
            public int To;
            /// <summary> Flow socket id of the From node, null for value connections. </summary>
            public string FlowSocket;
        }

        private List<SortEdge>[] CollectSortEdges()
        {
            var count = nodesToSerialize.Count;
            var edges = new List<SortEdge>[count];
            for (int i = 0; i < count; i++)
                edges[i] = new List<SortEdge>();

            bool IsValid(int? index) => index.HasValue && index.Value >= 0 && index.Value < count;

            foreach (var node in nodesToSerialize)
            {
                foreach (var valueSocket in node.ValueInConnection)
                    if (IsValid(valueSocket.Value.Node))
                        edges[valueSocket.Value.Node.Value].Add(new SortEdge { From = valueSocket.Value.Node.Value, To = node.Index });

                foreach (var flowSocket in node.FlowConnections)
                    if (IsValid(flowSocket.Value.Node))
                        edges[node.Index].Add(new SortEdge { From = node.Index, To = flowSocket.Value.Node.Value, FlowSocket = flowSocket.Key });
            }
            return edges;
        }

        /// <summary>
        /// Kahn's algorithm over value and flow connections. Among the nodes that are ready, the one with the lowest
        /// current index comes first, so the order stays as close as possible to the current one.
        /// </summary>
        /// <returns>false if the connections contain a cycle; <paramref name="sorted"/> then misses the nodes on or behind it.</returns>
        private bool TrySortByValuesAndFlows(List<SortEdge>[] edges, out LinkedList<int> sorted)
        {
            var count = nodesToSerialize.Count;
            var inDegree = new int[count];
            foreach (var edge in edges.SelectMany(e => e))
                inDegree[edge.To]++;

            var ready = new SortedSet<int>(Enumerable.Range(0, count).Where(i => inDegree[i] == 0));
            sorted = new LinkedList<int>();
            while (ready.Count > 0)
            {
                var node = ready.Min;
                ready.Remove(node);
                sorted.AddLast(node);
                foreach (var edge in edges[node])
                    if (--inDegree[edge.To] == 0)
                        ready.Add(edge.To);
            }
            return sorted.Count == count;
        }

        private LinkedList<int> SortByValuesAndFlows()
        {
            while (true)
            {
                var edges = CollectSortEdges();
                if (TrySortByValuesAndFlows(edges, out var sorted))
                    return sorted;

                var flowEdge = FindFlowEdgeInCycle(edges, new HashSet<int>(Enumerable.Range(0, nodesToSerialize.Count).Except(sorted)));
                if (!flowEdge.HasValue)
                {
                    Debug.LogError("Interactivity export: value connections form a cycle, nodes can't be ordered as required by the specification.");
                    return PostTopologicalSort();
                }

                // Only happens when a node triggers a node whose output value it (indirectly) reads.
                var edge = flowEdge.Value;
                var source = nodesToSerialize[edge.From];
                Debug.LogWarning($"Interactivity export: flow \"{edge.FlowSocket}\" of node {edge.From} ({source.Schema.Op}) " +
                                 $"to node {edge.To} ({nodesToSerialize[edge.To].Schema.Op}) is part of a value/flow cycle " +
                                 "and is routed through a custom event, so the target runs asynchronously.");
                RouteFlowThroughEvent(source, source.FlowConnections[edge.FlowSocket]);
            }
        }

        /// <summary> Finds a cycle between the given nodes and returns a flow connection on it (value connections can't be rerouted). </summary>
        private static SortEdge? FindFlowEdgeInCycle(List<SortEdge>[] edges, HashSet<int> nodes)
        {
            // 0 = not visited, 1 = on the current path, 2 = done
            var state = new Dictionary<int, int>();
            var path = new List<SortEdge>();

            SortEdge? Visit(int node)
            {
                state[node] = 1;
                foreach (var edge in edges[node])
                {
                    if (!nodes.Contains(edge.To))
                        continue;
                    state.TryGetValue(edge.To, out var targetState);
                    if (targetState == 1)
                    {
                        // Cycle: the edges on the path from edge.To to node, plus this edge
                        var flowEdge = path.SkipWhile(e => e.From != edge.To).Append(edge)
                            .Where(e => e.FlowSocket != null).Cast<SortEdge?>().FirstOrDefault();
                        if (flowEdge.HasValue)
                            return flowEdge;
                        // Value-only cycle; keep searching for a cycle with a flow connection
                        continue;
                    }
                    if (targetState == 0)
                    {
                        path.Add(edge);
                        var result = Visit(edge.To);
                        if (result.HasValue)
                            return result;
                        path.RemoveAt(path.Count - 1);
                    }
                }
                state[node] = 2;
                return null;
            }

            foreach (var node in nodes)
            {
                if (state.ContainsKey(node))
                    continue;
                var result = Visit(node);
                if (result.HasValue)
                    return result;
            }
            return null;
        }

        protected LinkedList<int> PostTopologicalSort()
        {
            var sorted = new LinkedList<int>();
            var visited = new Dictionary<int, bool>();

            void Visit(int node)
            {
                bool inProcess;
                bool alreadyVisited = visited.TryGetValue(node, out inProcess);
            
                if (alreadyVisited)
                {
                    if (inProcess)
                    {
                        // TODO: Should quit the topological sort and cancel the export
                        // throw new ArgumentException("Cyclic dependency found.");
                    }
                }
                else
                {
                    visited[node] = true;

                    // Get the dependencies from incoming connections and ignore self-references
                    HashSet<int> dependencies = new HashSet<int>();
                    var currentNode = nodesToSerialize[node];
                    foreach (var connection in currentNode.ValueInConnection)
                    {
                        if (connection.Value.Node != null && connection.Value.Node.HasValue)
                        {
                            dependencies.Add(connection.Value.Node.Value);
                        }
                    }

                    foreach (var dependency in dependencies)
                    {
                        Visit(dependency);
                    }

                    visited[node] = false;
                    sorted.AddLast(node);
                }
            }
            
            foreach (var node in nodesToSerialize)
            {
                Visit(node.Index);
            }

            return sorted;
        }
        
        protected GltfTypes.TypeMapping[] CollectAndFilterUsedTypes()
        {
            var types = new List<GltfTypes.TypeMapping>();
            
            // key = old index, value = new index
            var typesIndexReplacement = new Dictionary<int, int>();
            var usedTypeIndices = new HashSet<int>();

            // Collect used Types
            foreach (var variable in variables.Where(v => v.Type == -1 && v.Value != null))
            {
                var typeIndex = GltfTypes.TypeIndex(variable.Value.GetType());
                variable.Type = typeIndex;
                usedTypeIndices.Add(typeIndex);
            }
            
            foreach (var variable in variables.Where(v => v.Type != -1))
                usedTypeIndices.Add(variable.Type);

            foreach (var customEventValue in customEvents.SelectMany(c => c.Values))
                usedTypeIndices.Add(customEventValue.Value.Type);
            
            foreach (var declaration in 
                     opDeclarations.Where(d => d.inputValueSockets != null).SelectMany( d => d.inputValueSockets.Values)
                         .Concat(
                             opDeclarations.Where(d => d.outputValueSockets != null).SelectMany( d => d.outputValueSockets.Values)))
                usedTypeIndices.Add(declaration.type);
            
            foreach (var node in nodesToSerialize)
            {
                foreach (var config in node.Configuration)
                {
                    if (config.Key == "type" && config.Value.Value != null)
                    {
                        if ((int)config.Value.Value != -1)
                            usedTypeIndices.Add((int)config.Value.Value);
                        
                    }
                }
                foreach (var valueSocket in node.ValueInConnection)
                    if (valueSocket.Value.Value != null)
                    {
                        if (valueSocket.Value.Type == -1)
                            valueSocket.Value.Type = GltfTypes.TypeIndex(valueSocket.Value.Value.GetType());
                        usedTypeIndices.Add(valueSocket.Value.Type);
                    }
                    else if (valueSocket.Value.Node == null && valueSocket.Value.Type != -1)
                    {
                        // Type-default value, serialized with its type only
                        usedTypeIndices.Add(valueSocket.Value.Type);
                    }
                
                foreach (var outSocket in node.OutputValueSocket)
                    if (outSocket.Value.expectedType != null)
                    {
                        if (outSocket.Value.expectedType.typeIndex != null)
                            usedTypeIndices.Add(outSocket.Value.expectedType.typeIndex.Value);
                    }
            }
            
            // Create used Type Mapping List and mark the new indices
            foreach (var typeIndex in usedTypeIndices.OrderBy( t => t))
            {
                if (typeIndex < 0 || typeIndex >= GltfTypes.TypesMapping.Length)
                {
                    Debug.LogError("Type index out of range: " + typeIndex);
                    continue;
                }
                types.Add(GltfTypes.TypesMapping[typeIndex]);
                typesIndexReplacement.Add(typeIndex, types.Count - 1);
            }
            
            // Replace the old type indices with the new ones
            foreach (var node in nodesToSerialize)
            {
                foreach (var config in node.Configuration)
                {
                    if (config.Key == "type" && config.Value.Value != null)
                    {
                        if ((int)config.Value.Value != -1)
                            config.Value.Value = typesIndexReplacement[(int)config.Value.Value];
                    }
                }
                foreach (var valueSocket in node.ValueInConnection)
                    if ((valueSocket.Value.Value != null || valueSocket.Value.Node == null) && valueSocket.Value.Type != -1
                        && typesIndexReplacement.ContainsKey(valueSocket.Value.Type))
                        valueSocket.Value.Type = typesIndexReplacement[valueSocket.Value.Type];
            }
            
            foreach (var variable in variables.Where( v => v.Type != -1))
                variable.Type = typesIndexReplacement[variable.Type];

            var alreadyUpdated = new HashSet<GltfInteractivityNode.EventValues>();
            // In case EventValues instances are shared betweens multiple events, we use a hashset here
            foreach (var customEventValue in customEvents.SelectMany(c => c.Values))
            {
                if (!alreadyUpdated.Contains(customEventValue.Value))
                {
                    customEventValue.Value.Type = typesIndexReplacement[customEventValue.Value.Type];
                    alreadyUpdated.Add(customEventValue.Value);
                }
            }

            foreach (var declaration in opDeclarations.Where(d => d.inputValueSockets != null).SelectMany(d => d.inputValueSockets.Values)
                         .Concat(opDeclarations.Where(d => d.outputValueSockets != null).SelectMany(d => d.outputValueSockets.Values)))
                declaration.type = typesIndexReplacement[declaration.type];
            
            return types.ToArray();
        }
     
        protected void CheckForCircularFlows()
        {
            var visited = new Dictionary<int, bool>(nodesToSerialize.Count);
            
            bool Visit(int node)
            {
                if (visited.TryGetValue(node, out var alreadyVisited))
                {
                    if (alreadyVisited)
                        return true;
                }
                
                if (!alreadyVisited)
                {
                    visited[node] = true;

                    // Get the dependencies from incoming connections and ignore self-references
                    var currentNode = nodesToSerialize[node];
                    foreach (var connection in currentNode.FlowConnections)
                    {
                        if (connection.Value.Node != null && connection.Value.Node.HasValue && connection.Value.Node.Value < nodesToSerialize.Count)
                        {
                            if (Visit(connection.Value.Node.Value))
                            {
                                // Add Events because of cyclic dependency
                                RouteFlowThroughEvent(currentNode, connection.Value);
                            }
                        }
                    }

                    visited[node] = false;
                }

                return false;
            }
            
            foreach (var node in nodesToSerialize.ToArray())
                Visit(node.Index);

        }

        /// <summary>
        /// Replaces the flow connection with event/send → event/receive using a new custom event, which breaks cycles.
        /// Custom events are processed asynchronously, so the flow target no longer runs within the current flow.
        /// </summary>
        private void RouteFlowThroughEvent(GltfInteractivityExportNode sourceNode, GltfInteractivityNode.FlowSocketData connection)
        {
            var eventId = AddEventWithIdIfNeeded($"CyclicDependency{connection.Node.ToString()}from{sourceNode.Index.ToString()}");

            var triggerEventNode = new GltfInteractivityExportNode(new Event_SendNode());
            triggerEventNode.Index = nodesToSerialize.Count;
            triggerEventNode.Configuration["event"].Value = eventId;
            nodesToSerialize.Add(triggerEventNode);

            var receiveEventNode = new GltfInteractivityExportNode(new Event_ReceiveNode());
            receiveEventNode.Index = nodesToSerialize.Count;
            receiveEventNode.Configuration["event"].Value = eventId;
            nodesToSerialize.Add(receiveEventNode);

            var receiveFlowOut = receiveEventNode.FlowConnections[Event_ReceiveNode.IdFlowOut];
            receiveFlowOut.Node = connection.Node;
            receiveFlowOut.Socket = connection.Socket;

            connection.Node = triggerEventNode.Index;
            connection.Socket = Event_SendNode.IdFlowIn;
        }
        
        public void RemoveNode(GltfInteractivityExportNode nodeToRemove)
        {
            var indexToRemove = nodesToSerialize.IndexOf(nodeToRemove);
            if (indexToRemove == -1)
            {
                Debug.LogError("Can't remove Node, not found in list!");
                return;
            }
            // Safety check if there exist any connection to the removed node
            foreach (var n in nodesToSerialize)
            {
                foreach (var valueSocket in n.ValueInConnection)
                {
                    if (valueSocket.Value.Node == indexToRemove)
                    {
                        Debug.LogError("Trying to remove an node, which is referenced in a value connection. Schema: "+nodeToRemove.Schema.Op + ",  Referenced by " + n.Schema.Op);
                        return;
                    }
                }
                foreach (var flowSocket in n.FlowConnections)
                {
                    if (flowSocket.Value.Node == indexToRemove)
                    {
                        Debug.LogError("Trying to remove an node, which is referenced in a flow connection. Schema: "+nodeToRemove.Schema.Op + ",  Referenced by " + n.Schema.Op);
                        return;
                    }
                }
            }
            
            if (indexToRemove == nodesToSerialize.Count - 1)
            {
                // Just remove, no other indices are affected
                nodesToSerialize.RemoveAt(indexToRemove);
                return;
            }
                
            nodesToSerialize.RemoveAt(indexToRemove);
            // Move last node to the removed node index
            nodesToSerialize.Insert(indexToRemove, nodesToSerialize.Last());
            nodesToSerialize.RemoveAt(nodesToSerialize.Count - 1);
              
            int indexToReplace = nodesToSerialize[indexToRemove].Index;
            nodesToSerialize[indexToRemove].Index = nodeToRemove.Index;
            
            // Replace old index with new index of the inserted node
            foreach (var n in nodesToSerialize)
            {
                foreach (var valueSocket in n.ValueInConnection)
                {
                    if (valueSocket.Value.Node == indexToReplace)
                        valueSocket.Value.Node = nodeToRemove.Index;
                }
                foreach (var flowSocket in n.FlowConnections)
                {
                    if (flowSocket.Value.Node == indexToReplace)
                        flowSocket.Value.Node = nodeToRemove.Index;
                }
            }
        }
        
        protected void RemoveNodes(IReadOnlyList<GltfInteractivityExportNode> nodesToRemove)
        {
            foreach (var removedNode in nodesToRemove)
            {
                RemoveNode(removedNode);
            }
        }

        protected void CleanUp()
        {
            var nodeCountBefore = nodesToSerialize.Count;
            bool hasChanges = false;
            do
            {
                hasChanges = CleanUpRegistry.StartCleanUp(this);
            } while (hasChanges);
            
#if INTERACTIVITY_DEBUG_LOGS
            var nodeCountAfter = nodesToSerialize.Count;
            if (nodeCountBefore != nodeCountAfter)
            {
                Debug.Log($"Removed {nodeCountBefore - nodeCountAfter} nodes in cleanup.");
            }
#endif
        }

        protected void RemoveUnconnectedNodes()
        {
            var visited = new HashSet<int>(nodesToSerialize.Count);
            var nodesToRemove = new List<GltfInteractivityExportNode>();
           
            //Collect which nodes has connections
            foreach (var node in nodesToSerialize)
            {
                if (node.ValueInConnection.Count > 0)
                {
                    foreach (var valueSocket in node.ValueInConnection)
                    {
                        if (valueSocket.Value.Node != null && valueSocket.Value.Node != -1)
                        {
                            visited.Add(valueSocket.Value.Node.Value);
                            visited.Add(node.Index);
                        }
                    }
                }

                if (node.FlowConnections.Count > 0)
                {
                    foreach (var flowSocket in node.FlowConnections)
                    {
                        if (flowSocket.Value.Node != null && flowSocket.Value.Node != -1)
                        {
                            visited.Add(flowSocket.Value.Node.Value);
                            visited.Add(node.Index);
                        }
                    }
                }
            }

            foreach (var node in nodesToSerialize)
            {
                if (!visited.Contains(node.Index))
                    nodesToRemove.Add(node);
            }
            
            RemoveNodes(nodesToRemove);
        }
        
        private GltfInteractivityExportNode[] AddTypeConversion(GltfInteractivityExportNode targetNode, int conversionNodeIndex, string targetInputSocket, int fromType, int toType)
        {
            var newNodes = new List<GltfInteractivityExportNode>();
            
            var fromTypeSignature = GltfTypes.TypesMapping[fromType].GltfSignature;
            var toTypeSignature = GltfTypes.TypesMapping[toType].GltfSignature;

            var targetSocketData = targetNode.ValueInConnection[targetInputSocket];
            GltfInteractivityExportNode conversionNode = null;
            
            var fromTypeComponentCount = GltfTypes.GetComponentCount(fromTypeSignature);
            var toTypeComponentCount = GltfTypes.GetComponentCount(toTypeSignature);
            
            void SetupSimpleConversion(GltfInteractivityNodeSchema schema)
            {
                conversionNode = new GltfInteractivityExportNode(schema);
                conversionNode.Index = conversionNodeIndex;
                newNodes.Add(conversionNode);
                conversionNode.ValueInConnection["a"] = new GltfInteractivityNode.ValueSocketData()
                {
                    Node = targetSocketData.Node, 
                    Socket = targetSocketData.Socket, 
                    Value = targetSocketData.Value, 
                    Type = targetSocketData.Type
                };
                
                targetSocketData.Node = conversionNodeIndex;
                targetSocketData.Socket = "value";
                targetSocketData.Value = null;
            }
            
            void SetupConversion(GltfInteractivityNodeSchema schema)
            {
                conversionNode= new GltfInteractivityExportNode(schema);
                conversionNode.Index = conversionNodeIndex;
                newNodes.Add(conversionNode);

                int indexForTargetNodeInput = conversionNodeIndex;
                
                if (fromTypeComponentCount == 1 && toTypeComponentCount > 1)
                {
                    foreach (var input in conversionNode.ValueInConnection)
                    {
                        input.Value.Node = targetSocketData.Node;
                        input.Value.Socket = targetSocketData.Socket;
                        input.Value.Value = targetSocketData.Value;
                        input.Value.Type = targetSocketData.Type;
                    }
                }
                else if (toTypeComponentCount > 1)
                {
                    GltfInteractivityNodeSchema extractSchema = null;
                    switch (fromTypeComponentCount)
                    {
                        case 2:
                            extractSchema = new Math_Extract2Node();
                            break;
                        case 3:
                            extractSchema = new Math_Extract3Node();
                            break;
                        case 4:
                            extractSchema = new Math_Extract4Node();
                            break;
                    }

                    if (extractSchema == null)
                    {
                       newNodes.Clear();
                       return;
                    }
                    
                    var extractNode = new GltfInteractivityExportNode(extractSchema);
                    extractNode.Index = conversionNodeIndex + newNodes.Count;
                    newNodes.Add(extractNode);
                    
                    var extractInput = extractNode.ValueInConnection["a"];
                    extractInput.Node = targetSocketData.Node;
                    extractInput.Socket = targetSocketData.Socket;
                    extractInput.Value = targetSocketData.Value;
                    extractInput.Type = targetSocketData.Type;

                    int inputExtractIndex = 0;
                    foreach (var inputSocket in conversionNode.ValueInConnection)
                    {
                        if (inputExtractIndex <= extractNode.OutputValueSocket.Count - 1)
                        {
                            inputSocket.Value.Node = extractNode.Index;
                            inputSocket.Value.Socket = inputExtractIndex.ToString();
                            inputExtractIndex++;
                        }
                        else
                        {
                            inputSocket.Value.Value = 0f;
                            inputSocket.Value.Type = GltfTypes.TypeIndexByGltfSignature("float");
                        }
                    }
                }
                
                targetSocketData.Node = indexForTargetNodeInput;
                targetSocketData.Socket = "value";
                targetSocketData.Value = null;
            }
            
            if (fromTypeSignature == toTypeSignature)
                return null;
            
            var conversionSchema = GltfTypes.GetTypeConversionSchema(fromTypeSignature, toTypeSignature);
            if (conversionSchema != null)
            {
                if (conversionSchema.InputValueSockets.Count == 1)
                    SetupSimpleConversion(conversionSchema);
                else
                    SetupConversion(conversionSchema);
            }

            return newNodes.ToArray();
        }

        public void CheckForImplicitValueConversions()
        {
            var changed = true;
            while (changed)
            {
                changed = false;
                
                foreach (var node in nodesToSerialize.ToArray())
                {
                    foreach (var valueSocket in node.ValueInConnection)
                    {
                        var socket = valueSocket.Value;

                        if (valueSocket.Value.Node == null && valueSocket.Value.Value == null)
                        {
                            // Try to handle nulls

                            if (socket.typeRestriction != null)
                            {
                                if (socket.typeRestriction.limitToType != null)
                                {
                                    valueSocket.Value.Value =
                                        GltfTypes.GetNullByType(socket.typeRestriction.limitToType);
                                    valueSocket.Value.Type =
                                        GltfTypes.TypeIndexByGltfSignature(socket.typeRestriction.limitToType);
                                }
                                else if (socket.typeRestriction.fromInputPort != null)
                                {
                                    var fromInputPort = socket.typeRestriction.fromInputPort;
                                    var fromInputPortType = GetValueTypeForInput(node, fromInputPort);
                                    if (fromInputPortType != -1)
                                    {
                                        valueSocket.Value.Value = GltfTypes.GetNullByType(fromInputPortType);
                                        valueSocket.Value.Type = fromInputPortType;
                                    }
                                }
                            }
                        }

                        if (socket != null && socket.typeRestriction != null)
                        {
                            var valueType = GetValueTypeForInput(node, valueSocket.Key);
                            if (valueType == -1)
                                continue;
                            if (socket.typeRestriction.limitToType != null)
                            {
                                var limitToType =
                                    GltfTypes.TypeIndexByGltfSignature(socket.typeRestriction
                                        .limitToType);
                                if (limitToType != valueType)
                                {

                                    if (socket.Value != null)
                                    {
                                        if (GltfTypes.TryToConvertValue(socket.Value,
                                                socket.typeRestriction.limitToType, out var convertedValue))
                                        {
                                            socket.Value = convertedValue;
                                            socket.Type = GltfTypes.TypeIndexByGltfSignature(socket.typeRestriction.limitToType);
                                            changed = true;
                                            continue;
                                        }
                                    }
                                    
                                    var conversionNode = AddTypeConversion(node, nodesToSerialize.Count,
                                        valueSocket.Key,
                                        valueType, limitToType);
                                    if (conversionNode != null && conversionNode.Length > 0)
                                    {
                                        nodesToSerialize.AddRange(conversionNode);
                                        changed = true;
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Could not add type conversion for socket: " + valueSocket.Key +
                                            " in node: " + node.Schema.Op + ". Has Type " + GltfTypes.TypesMapping[valueType].GltfSignature +
                                            " but should be " + GltfTypes.TypesMapping[limitToType].GltfSignature);;
                                    }
                                }
                            }
                            else if (socket.typeRestriction.fromInputPort != null)
                            {
                                var fromInputPort = socket.typeRestriction.fromInputPort;
                                var fromInputPortType = GetValueTypeForInput(node, fromInputPort);
                                if (fromInputPortType == -1)
                                    continue;
                                if (fromInputPortType != valueType)
                                {
                                    var preferType =
                                        GltfTypes.PreferType(valueType, fromInputPortType);
                                    if (preferType == -1 || preferType == valueType)
                                    {
                                        continue;
                                    }
                                    
                                    if (socket.Value != null)
                                    {
                                        if (GltfTypes.TryToConvertValue(socket.Value,
                                                GltfTypes.TypesMapping[fromInputPortType].GltfSignature, out var convertedValue))
                                        {
                                            socket.Value = convertedValue;
                                            socket.Type = fromInputPortType;
                                            changed = true;
                                            continue;
                                        }
                                    }

                                    var conversionNode = AddTypeConversion(node, nodesToSerialize.Count,
                                        valueSocket.Key,
                                        valueType, preferType);
                                    if (conversionNode != null && conversionNode.Length > 0)
                                    {
                                        changed = true;
                                        nodesToSerialize.AddRange(conversionNode);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Could not add type conversion for socket: " + valueSocket.Key +
                                                         " in node: " + node.Schema.Op + ". Has Type " + GltfTypes.TypesMapping[valueType].GltfSignature +
                                                         " but should be " + GltfTypes.TypesMapping[fromInputPortType].GltfSignature);;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
        
        public int GetValueTypeForOutput(GltfInteractivityExportNode node, string socketName)
        {
            if (!node.OutputValueSocket.TryGetValue(socketName, out var socketData))
                return -1;

            int nodeIndex = nodesToSerialize.IndexOf(node);
            for (int i = 0; i < nodesToSerialize.Count; i++)
            {
                var otherNode = nodesToSerialize[i];
                if (otherNode == node)
                    continue;

                foreach (var otherSocketData in otherNode.ValueInConnection.Values)
                {
                    if (otherSocketData.Node == nodeIndex && otherSocketData.Socket == socketName)
                    {
                        if (otherSocketData.Type != -1)
                            return otherSocketData.Type;

                        if (otherSocketData.typeRestriction != null)
                        {
                            if (!string.IsNullOrEmpty(otherSocketData.typeRestriction.limitToType))
                            {
                                return GltfTypes.TypeIndexByGltfSignature(otherSocketData.typeRestriction.limitToType);
                            }

                            if (!string.IsNullOrEmpty(otherSocketData.typeRestriction.fromInputPort))
                            {
                                var inputType = GetValueTypeForInput(otherNode,
                                    otherSocketData.typeRestriction.fromInputPort);
                                if (inputType != -1)
                                    return inputType;

                            }
                        }
                    }
                }
            }

            return -1;
        }
        
        public int GetValueTypeForInput(GltfInteractivityNode node, string socketName, HashSet<GltfInteractivityNode.ValueSocketData> visited = null)
        {
            if (visited == null)
                visited = new HashSet<GltfInteractivityNode.ValueSocketData>();
            
            if (!node.ValueInConnection.TryGetValue(socketName, out var socketData))
                return -1;
            
            if (visited.Contains(socketData))
                return -1;
            visited.Add(socketData);
            
            if (socketData.Type != -1 && socketData.Value != null && GltfTypes.TypeIndex(socketData.Value.GetType()) == socketData.Type)
                return socketData.Type;
            
            if (socketData.Node != null)
            {
                var nodeIndex = socketData.Node.Value;
                if (nodeIndex >= 0 && nodeIndex < nodesToSerialize.Count)
                {
                    var inputSourceNode = nodesToSerialize[nodeIndex];
                    if (inputSourceNode.OutputValueSocket.TryGetValue(socketData.Socket,
                            out var sourceNodeOutSocketData))
                    {
                        
                        if (sourceNodeOutSocketData.expectedType != null)
                        {
                            if (sourceNodeOutSocketData.expectedType.typeIndex != null)
                                return sourceNodeOutSocketData.expectedType.typeIndex.Value;

                            if (sourceNodeOutSocketData.expectedType.fromInputPort != null)
                                return GetValueTypeForInput(inputSourceNode, sourceNodeOutSocketData.expectedType.fromInputPort, visited);
                        }
                    }
                    
                }

                return -1;
            }
            
            if (socketData.typeRestriction != null)
            {
                if (!string.IsNullOrEmpty(socketData.typeRestriction.limitToType))
                    return GltfTypes.TypeIndexByGltfSignature(socketData.typeRestriction.limitToType);

                if (!string.IsNullOrEmpty(socketData.typeRestriction.fromInputPort))
                {
                    int typeFromInput = GetValueTypeForInput(node, socketData.typeRestriction.fromInputPort, visited);
                    if (typeFromInput != -1)
                        return typeFromInput;
                }
            }
            
            if (socketData.Value != null)
                return GltfTypes.TypeIndex(socketData.Value.GetType());
            return socketData.Type;
        }
        
        public void CollectOpDeclarations()
        {
            int IndexOfNodeOp(GltfInteractivityNode node)
            {
                for (int i = 0; i < opDeclarations.Count; i++)
                {
                    if (opDeclarations[i].op.Equals(node.Schema.Op))
                    {
                        return i;
                    }
                }

                return -1;
            }

            int IndexOfOp(GltfInteractivityGraph.Declaration newDeclaration)
            {
                for (int i = 0; i < opDeclarations.Count; i++)
                {
                    if (opDeclarations[i].op.Equals(newDeclaration.op))
                    {
                        if (!string.IsNullOrEmpty(newDeclaration.extension))
                        {
                            var opInputs = opDeclarations[i].inputValueSockets;
                            var opOutputs = opDeclarations[i].outputValueSockets;
                            
                            // Compare the input and output sockets if they match with exisiting ones
                            
                            foreach (var input in newDeclaration.inputValueSockets)
                            {
                                if (!opInputs.TryGetValue(input.Key, out var opInput))
                                    return -1;

                                if (opInput.type != input.Value.type)
                                    return -1;
                            }
                            
                            foreach (var output in newDeclaration.outputValueSockets)
                            {
                                if (!opOutputs.TryGetValue(output.Key, out var opOutput))
                                    return -1;

                                if (opOutput.type != output.Value.type)
                                    return -1;
                            }

                        }
                        return i;
                    }
                }

                return -1;
            }
            
            foreach (var node in nodesToSerialize)
            {
                var opIndex = IndexOfNodeOp(node);
                if (opIndex == -1)
                {
                    var newDeclaration = new GltfInteractivityGraph.Declaration();
                    opIndex = opDeclarations.Count;
                    opDeclarations.Add(newDeclaration);
                    
                    newDeclaration.op = node.Schema.Op;
                    if (!string.IsNullOrEmpty(node.Schema.Extension))
                    {
                        newDeclaration.extension = node.Schema.Extension;
                        var inputs = new Dictionary<string, GltfInteractivityGraph.Declaration.ValueSocket>();
                        foreach (var input in node.ValueInConnection)
                        {
                            var schemaInput = node.Schema.InputValueSockets.FirstOrDefault(i => i.Key == input.Key);
                            var newInput = new GltfInteractivityGraph.Declaration.ValueSocket { type = GetValueTypeForInput(node, input.Key)};
                            if (newInput.type == -1)
                            {
                                // Probably it has no connection, so we use the Schema Type
                                if (input.Value.typeRestriction != null)
                                {
                                    if (!string.IsNullOrEmpty(input.Value.typeRestriction.limitToType))
                                        newInput.type = GltfTypes.TypeIndexByGltfSignature(input.Value.typeRestriction.limitToType);
                                    else
                                    {
                                        var typeFromOtherPort = GetValueTypeForInput(node, input.Value.typeRestriction.fromInputPort);
                                        if (typeFromOtherPort != -1)
                                            newInput.type = typeFromOtherPort;
                                    }

                                }
                                if (newInput.type == -1 && schemaInput.Value != null && schemaInput.Value.SupportedTypes.Length > 0)
                                    newInput.type = GltfTypes.TypeIndexByGltfSignature(schemaInput.Value.SupportedTypes[0]);
                                
                                if (newInput.type == -1)
                                    Debug.LogError("Declaration invalid: Could not resolve Type for Input: "+input.Key + " in Node: "+node.Schema.Op);
                            }

                            inputs.Add(input.Key, newInput);
                        }
                        newDeclaration.inputValueSockets = inputs;
                        
                        var outputs = new Dictionary<string, GltfInteractivityGraph.Declaration.ValueSocket>();
                        
                        foreach (var output in node.OutputValueSocket)
                        {
                            var schemaOutput = node.Schema.OutputValueSockets.FirstOrDefault(i => i.Key == output.Key);

                            var newOutput = new GltfInteractivityGraph.Declaration.ValueSocket();
                            newOutput.type = -1;
                            if (output.Value.expectedType != null)
                            {
                                if (output.Value.expectedType.typeIndex != null)
                                    newOutput.type = output.Value.expectedType.typeIndex.Value;
                                else
                                {
                                    var fromInputPortType = GetValueTypeForInput(node, output.Value.expectedType.fromInputPort);
                                    if (fromInputPortType != -1)
                                        newOutput.type = fromInputPortType;
                                }
                            }

                            if (newOutput.type == -1 && schemaOutput.Value != null && schemaOutput.Value.SupportedTypes.Length > 0)
                            {
                                newOutput.type =
                                    GltfTypes.TypeIndexByGltfSignature(schemaOutput.Value.SupportedTypes[0]);
                            }
                            
                            if (newOutput.type == -1)
                                Debug.LogError("Declaration invalid: Could not resolve Type for Output: "+output.Key + " in Node: "+node.Schema.Op);
                            
                            outputs.Add(output.Key,  newOutput);
                        }
                        newDeclaration.outputValueSockets = outputs;
                    }

                    var opDeclarationIndex = IndexOfOp(newDeclaration);
                    if (opDeclarationIndex == -1)
                        opDeclarations.Add(newDeclaration);
                    else 
                        opIndex = opDeclarationIndex;
                }

                node.OpDeclaration = opIndex;
            }
        }
        
        protected void AddSelectabilityExtensionToInvisibleNodes()
        {
            // Ensure initial invisible nodes also gets unselectable 
            
            foreach (var node in this.exporter.GetRoot().Nodes)
            {
                if (node.Extensions != null && node.Extensions.TryGetValue(KHR_node_visibility_Factory.EXTENSION_NAME, out var nodeVisibilityExtension))
                {
                    var visible = (nodeVisibilityExtension as KHR_node_visibility).visible;
                    if (!visible)
                        AddSelectabilityExtensionToNode(node, visible);
                }
            }
        }
    }
}