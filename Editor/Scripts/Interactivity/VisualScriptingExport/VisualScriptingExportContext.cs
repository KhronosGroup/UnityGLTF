using System.Text;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace UnityGLTF.Interactivity.VisualScripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using UnityEngine;
    using Unity.VisualScripting;
    using UnityGLTF;
    using UnityGLTF.Plugins;
    
    public class VisualScriptingExportContext: GLTFExportPluginContext
    {
        #region Classes
        public class InputPortGraph 
        {
            public IUnitInputPort port;
            public ExportGraph graph;
            
            public override int GetHashCode()
            {
                string s = $"{port.key}+{port.unit.ToString()}+{graph.graph.title}+{graph.graph.GetHashCode()}";

                return s.GetHashCode();
            }
            
            public InputPortGraph(IUnitInputPort port, ExportGraph graph)
            {
                this.port = port;
                this.graph = graph;
            }
        }
        
        public class InputportGraphComparer : IEqualityComparer<InputPortGraph>
        {
            public bool Equals(InputPortGraph x, InputPortGraph y)
            {
                return x.port == y.port && x.graph == y.graph;
            }

            public int GetHashCode(InputPortGraph obj)
            {
                return obj.GetHashCode();
            }
        }
        
        public class ExportGraph
        {
            public GameObject gameObject = null;
            public ExportGraph parentGraph = null;
            public FlowGraph graph;
            public Dictionary<IUnit, UnitExporter> nodes = new Dictionary<IUnit, UnitExporter>();
            internal Dictionary<IUnitInputPort, IUnitInputPort> bypasses = new Dictionary<IUnitInputPort, IUnitInputPort>();
            public List<ExportGraph> subGraphs = new List<ExportGraph>();
        }
        #endregion
        
        public ScriptMachine ActiveScriptMachine = null;
        public GLTFRoot ActiveGltfRoot = null;

        public GLTFSceneExporter exporter { get; private set; }
        
        internal List<GltfInteractivityGraph.Variable> variables = new List<GltfInteractivityGraph.Variable>();
        internal List<GltfInteractivityGraph.CustomEvent> customEvents = new List<GltfInteractivityGraph.CustomEvent>();
        internal List<GltfInteractivityGraph.Declaration> opDeclarations = new List<GltfInteractivityGraph.Declaration>();
        private List<UnitExporter> nodesToExport = new List<UnitExporter>();
        
        public delegate void OnBeforeSerializationDelegate(List<GltfInteractivityNode> nodes);
        public delegate void OnNodesCreatedDelegate(List<GltfInteractivityNode> nodes);
        public event OnBeforeSerializationDelegate OnBeforeSerialization;
        public event OnNodesCreatedDelegate OnNodesCreated;
        
        internal Dictionary<InputPortGraph, InputPortGraph> graphBypasses = new Dictionary<InputPortGraph, InputPortGraph>(new InputportGraphComparer());
        internal Dictionary<(IUnitInputPort port, SubgraphUnit subGraph), (IUnitInputPort port, ExportGraph graph)> bypassesSubGraphs = new Dictionary<(IUnitInputPort, SubgraphUnit), (IUnitInputPort, ExportGraph)>();
        private List<ExportGraph> addedGraphs = new List<ExportGraph>();
        private List<VariableBasedList> addedVariableBasedLists = new List<VariableBasedList>();
               
        private List<GltfInteractivityNode> nodesToSerialize = new List<GltfInteractivityNode>();
        public List<GltfInteractivityNode> Nodes
        {
            get => nodesToSerialize;
        }
        
        internal ExportGraph currentGraphProcessing { get; private set; } = null;
        
        public VisualScriptingExportPlugin plugin;
        
        public VisualScriptingExportContext(VisualScriptingExportPlugin plugin)
        {
            this.plugin = plugin;
        }

        public VariableBasedList GetListByCreator(IUnit listCreatorUnit)
        {
            return addedVariableBasedLists
                .Where( v => v is VariableBasedListFromUnit)
                .Cast<VariableBasedListFromUnit>()
                .FirstOrDefault(l => l.listCreatorUnit == listCreatorUnit && l.listCreatorGraph == currentGraphProcessing);
        }
        
        public VariableBasedList GetListByCreator(VariableDeclaration variable)
        {
            return addedVariableBasedLists
                .Where( v => v is VariableBasedListFromGraphVariable)
                .Cast<VariableBasedListFromGraphVariable>()
                .FirstOrDefault(l => l.varDeclarationSource == variable && l.listCreatorGraph == currentGraphProcessing);
        }
        
        public VariableBasedListFromUnit CreateNewVariableBasedListFromUnit(Unit listCreatorUnit, int capacity, int gltfType, string listId = null)
        {
            if (string.IsNullOrEmpty(listId))
                listId = Guid.NewGuid().ToString();
            var newVariableBasedList = new VariableBasedListFromUnit(listCreatorUnit, this, listId, capacity, gltfType);
            addedVariableBasedLists.Add(newVariableBasedList);
            newVariableBasedList.listCreatorGraph = currentGraphProcessing;
            return newVariableBasedList;
        }
        
        public VariableBasedListFromGraphVariable CreateNewVariableBasedListFromVariable(VariableDeclaration varDeclaration, int capacity, int gltfType, string listId = null)
        {
            if (string.IsNullOrEmpty(listId))
                listId = Guid.NewGuid().ToString();
            var newVariableBasedList = new VariableBasedListFromGraphVariable(varDeclaration,this, listId, capacity, gltfType);
            addedVariableBasedLists.Add(newVariableBasedList);
            newVariableBasedList.listCreatorGraph = currentGraphProcessing;
            return newVariableBasedList;
        }

        /// <summary>
        /// Get the value of a variable from a VariableUnit.
        /// Materials and GameObjects Values will be converted to their respective indices.
        /// </summary>
        public object GetVariableValue(IUnifiedVariableUnit unit, out string varName, out string cSharpVarType, bool checkTypeIsSupported = true)
        {
            var rawValue = GetVariableValueRaw(unit, out varName, out cSharpVarType, checkTypeIsSupported);
            
            if (rawValue is GameObject gameObjectValue)
                rawValue = exporter.GetTransformIndex(gameObjectValue.transform);
            else if (rawValue is Component component)
                rawValue = exporter.GetTransformIndex(component.transform);
            else if (rawValue is Material materialValue)
                rawValue = exporter.GetMaterialIndex(materialValue);

            return rawValue;
        }
        
        public VariableDeclaration GetVariableDeclaration(IUnifiedVariableUnit unit)
        {
            string varName = unit.name.unit.defaultValues["name"] as string;
            VariableDeclarations varDeclarations = null;
            switch (unit.kind)
            {
                case VariableKind.Flow:
                case VariableKind.Graph:
                    varDeclarations = unit.graph.variables;
                    break;
                case VariableKind.Object:
                    var gameObject = UnitsHelper.GetGameObjectFromValueInput(unit.valueInputs["object"], unit.defaultValues, this);
                    if (gameObject != null)
                    {
                        varDeclarations = Variables.Object(gameObject);
                    }
                    break;
                case VariableKind.Scene:
                    varDeclarations = Variables.ActiveScene;
                    break;
                case VariableKind.Application:
                    varDeclarations = Variables.Application;
                    break;
                case VariableKind.Saved:
                    varDeclarations = Variables.Saved;
                    break;
            }

            if (varDeclarations == null)
                return null;
            
            return varDeclarations.GetDeclaration(varName);
        }
        
        /// <summary>
        /// Get the value of a variable from a VariableUnit.
        /// Materials and GameObjects Values will be returned as is.
        /// </summary>
        public object GetVariableValueRaw(IUnifiedVariableUnit unit, out string exportVarName, out string cSharpVarType, bool checkTypeIsSupported = true)
        {
            string varName = unit.name.unit.defaultValues["name"] as string;

            exportVarName = varName;
            object varValue = null;
            cSharpVarType = null;

            VariableDeclarations varDeclarations = null; 
            
            switch (unit.kind)
            {
                case VariableKind.Flow:
                case VariableKind.Graph:
                    varDeclarations = unit.graph.variables;
                    break;
                case VariableKind.Object:

                    var gameObject = UnitsHelper.GetGameObjectFromValueInput(unit.valueInputs["object"], unit.defaultValues, this);
                    if (gameObject != null)
                    {
                        varDeclarations = Variables.Object(gameObject);

                        var gameObjectIndex =exporter.GetTransformIndex(gameObject.transform);
                        exportVarName = $"node_{gameObjectIndex}_{varName}";
                    }
                    
                    break;
                case VariableKind.Scene:
                    varDeclarations = Variables.ActiveScene;
                    break;
                case VariableKind.Application:
                    varDeclarations = Variables.Application;
                    break;
                case VariableKind.Saved:
                    varDeclarations = Variables.Saved;
                    break;
            }
            
            if (varDeclarations != null)
            {
                var varDeclaration = varDeclarations.GetDeclaration(varName);
                if (varDeclaration != null)
                {
                    varValue = varDeclaration.value;
                    cSharpVarType = varDeclaration.typeHandle.Identification;
                }
                else
                {
                    UnitExportLogging.AddErrorLog(unit, "Variable not found");
                    Debug.LogError("Variable not found: " + varName);
                    return null;
                }
            }
            else
            {
                UnitExportLogging.AddErrorLog(unit, "Variable not found");
                Debug.LogError("Variable not found: " + varName);
                return null;
            }

            if (cSharpVarType == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Unkknown variable type");
                return null;
            }

            if (checkTypeIsSupported)
            {
                var typeIndex = GltfTypes.TypeIndex(cSharpVarType);
                if (typeIndex == -1)
                {
                    UnitExportLogging.AddErrorLog(unit, "Unsupported type");

                    Debug.LogError("Unsupported type for variable: " + cSharpVarType);
                    return null;
                }
            }
            
            return varValue;
        }

        public int GetValueTypeForOutput(GltfInteractivityNode node, string socketName)
        {
            if (!node.OutValueSocket.TryGetValue(socketName, out var socketData))
                return -1;

            int nodeIndex = nodesToSerialize.IndexOf(node);
            for (int i = 0; i < nodesToSerialize.Count; i++)
            {
                var otherNode = nodesToSerialize[i];
                if (otherNode == node)
                    continue;

                foreach (var otherSocketData in otherNode.ValueSocketConnectionData.Values)
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
            
            if (!node.ValueSocketConnectionData.TryGetValue(socketName, out var socketData))
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
                    if (inputSourceNode.OutValueSocket.TryGetValue(socketData.Socket,
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
        
        public int AddVariableIfNeeded(IUnifiedVariableUnit unit)
        {
            var varValue = GetVariableValue(unit, out string varName, out string cSharpVarType);

            if (GltfTypes.TypeIndex(cSharpVarType) == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Type not supported for variable: " + cSharpVarType);
            }
            
            var variableIndex = AddVariableWithIdIfNeeded(varName, varValue, unit.kind, cSharpVarType);
            return variableIndex;
        }

        public int AddVariableWithIdIfNeeded(string id, object defaultValue, VariableKind varKind, string cSharpVarType)
        {
            return AddVariableWithIdIfNeeded(id, defaultValue, varKind, GltfTypes.TypeIndex(cSharpVarType));
        }
        
        public int AddVariableWithIdIfNeeded(string id, object defaultValue, VariableKind varKind, Type type)
        {
            return AddVariableWithIdIfNeeded(id, defaultValue, varKind, GltfTypes.TypeIndex(type));
        }
        
        public int AddVariableWithIdIfNeeded(string id, object defaultValue, VariableKind varKind, int gltfTypeIndex)
        {
            if (gltfTypeIndex == -1)
            {
                if (defaultValue != null)
                    Debug.Log("Type not supported for variable: " + defaultValue.GetType().Name);
                return -1;
            }
            
            if (addedGraphs.Count > 0)
            {
                switch (varKind)
                {
                    case VariableKind.Flow:
                    case VariableKind.Graph:
                        id = $"graph{addedGraphs.FindIndex(ag => ag == currentGraphProcessing)}_{id}";
                        break;
                }
            }
            
            var index = variables.FindIndex(v => v.Id == id);
            if (index != -1)
                return index;

            GltfInteractivityGraph.Variable newVariable = new GltfInteractivityGraph.Variable();
            newVariable.Id = id;
            
            newVariable.Type = gltfTypeIndex;
            
            newVariable.Value = defaultValue;
            variables.Add(newVariable);
            return variables.Count - 1;
        }
        
        public int AddEventIfNeeded(Unit eventUnit, Dictionary<string, GltfInteractivityNode.EventValues> arguments = null)
        {
            var eventId = eventUnit.defaultValues["name"] as string;
            if (string.IsNullOrEmpty(eventId))
            {
                UnitExportLogging.AddErrorLog(eventUnit, "No event selected.");
                Debug.LogError("Event Id is empty");
                return -1;
            }
            
            return AddEventWithIdIfNeeded(eventId, arguments);
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
        
        public void AddSelectabilityExtensionToNode(int nodeIndex)
        {
            if (nodeIndex == -1)
                return;

            var nodeExtensions = ActiveGltfRoot.Nodes[nodeIndex].Extensions;
            if (nodeExtensions == null)
            {
                nodeExtensions = new Dictionary<string, IExtension>();
                ActiveGltfRoot.Nodes[nodeIndex].Extensions = nodeExtensions;
            }
            if (!nodeExtensions.ContainsKey(KHR_node_selectability_Factory.EXTENSION_NAME))
            {
                nodeExtensions.Add(KHR_node_selectability_Factory.EXTENSION_NAME, new KHR_node_selectability());
            }
            exporter.DeclareExtensionUsage(KHR_node_selectability_Factory.EXTENSION_NAME, false);
        }
        
        public void AddSelectabilityExtensionToAllNode()
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
        
        internal ExportGraph AddGraph(FlowGraph graph)
        {
            var newExportGraph = new ExportGraph();
            newExportGraph.gameObject = ActiveScriptMachine.gameObject;
            newExportGraph.parentGraph = currentGraphProcessing;
            newExportGraph.graph = graph;
            addedGraphs.Add(newExportGraph);
            if (currentGraphProcessing != null)
                currentGraphProcessing.subGraphs.Add(newExportGraph);
            
            var lastCurrentGraph = currentGraphProcessing;
            currentGraphProcessing = newExportGraph;
            // Topologically sort the graph to establish the dependency order
            
            // Keep an eye on it if topologicallySortedNodes are really not needed anymore
            //LinkedList<IUnit> topologicallySortedNodes = TopologicalSort(graph.units);
            
            var translatableUnits = UnitsHelper.GetTranslatableUnits(graph.units, this);
            
            // Order nodesToExport by priority, e.g. List/Array nodes should be exported first,
            // so other nodes which are required exisiting List/Array creators can find them
            var priorityOrdered = translatableUnits.Select( kvp => kvp.Value).OrderBy(n => n.unitExportPriority);
            foreach (var export in priorityOrdered)
            {
                export.InitializeInteractivityNode();
                if (export.IsTranslatable && export.Nodes.Length > 0)
                    newExportGraph.nodes.Add(export.unit, export);
            }
            
            // // Sort newExportGraph.nodes by topologicallySortedNodes
            // var sortedNodes = new Dictionary<IUnit, UnitExporter>();
            // foreach (var node in topologicallySortedNodes)
            // {
            //     if (newExportGraph.nodes.TryGetValue(node, out var exporter))
            //         sortedNodes.Add(node, exporter);
            // }
            
            nodesToExport.AddRange(newExportGraph.nodes.Select( g => g.Value));
            currentGraphProcessing = lastCurrentGraph;
            return newExportGraph;
        }
        
        private void TriggerInterfaceExportCallbacks()
        {
            GltfInteractivityExportNodes nodesExport = new GltfInteractivityExportNodes(nodesToSerialize);
            
            foreach (var root in exporter.RootTransforms)
            {
                var interfaces = root.GetComponentsInChildren<IInteractivityExport>(true);
                foreach (var callback in interfaces)
                {
                    callback.OnInteractivityExport(this, nodesExport);
                }                
            }
        }
        
        /// <summary>
        /// Called after the scene has been exported to add interactivity data.
        ///
        /// This overload of AfterSceneExport exposes the origins as a parameter to simplify tests.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        /// <param name="visualScriptingComponents"> list of ScriptMachines in the scene.</param>
        internal void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, List<ScriptMachine> visualScriptingComponents)
        {
            if (visualScriptingComponents.Count == 0)
            {
                return;
            }
            this.exporter = exporter;
            
            foreach (var scriptMachine in visualScriptingComponents)
            {
                ActiveScriptMachine = scriptMachine;
                FlowGraph flowGraph = scriptMachine.graph;
                AddGraph(flowGraph);
            }
            
            nodesToSerialize = nodesToExport.SelectMany(exportNode => exportNode.Nodes).Cast<GltfInteractivityNode>().ToList();

            for (int i = 0; i < nodesToSerialize.Count; i++)
                nodesToSerialize[i].Index = i;
            
            foreach (var graph in addedGraphs)
            {
                foreach (var exportNode in graph.nodes)
                    exportNode.Value.ResolveDefaultAndLiterals();
            }
            
            foreach (var graph in addedGraphs)
            {
                foreach (var exportNode in graph.nodes)
                    exportNode.Value.ResolveConnections();
            }
            
            OnNodesCreated?.Invoke(nodesToSerialize);
            
            RemoveUnconnectedNodes();

            TriggerInterfaceExportCallbacks();
            
            CheckForImplicitValueConversions();
            
            CheckForCircularFlows();
            
            if (plugin.cleanUpAndOptimizeExportedGraph)
                CleanUp();
            
            PostIndexTopologicalSort();  
            
            CollectOpDeclarations();
            
            OnBeforeSerialization?.Invoke(nodesToSerialize);
            // Clear the events
            OnBeforeSerialization = null;
            // Create the extension and add nodes to it
            GltfInteractivityExtension extension = new GltfInteractivityExtension();
            GltfInteractivityGraph mainGraph = new GltfInteractivityGraph();
            extension.graphs = new GltfInteractivityGraph[] {mainGraph};
            mainGraph.Nodes = nodesToSerialize.ToArray();
            mainGraph.Types = CollectAndFilterUsedTypes();
            
            ValidateData();
            
            mainGraph.Variables = variables.ToArray();
            mainGraph.CustomEvents = customEvents.ToArray();
            mainGraph.Declarations = opDeclarations.ToArray();
            
            gltfRoot.AddExtension(GltfInteractivityExtension.ExtensionName, extension);
            
            exporter.DeclareExtensionUsage(GltfInteractivityExtension.ExtensionName);
            
            OutputUnitLogs();
        }

        /// <summary>
        /// Called after the scene has been exported to add interactivity data.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            ActiveGltfRoot = gltfRoot;
            UnitExportLogging.ClearLogs();

            var scriptMachines = new List<ScriptMachine>();
            
            foreach (var root in exporter.RootTransforms)
            {
                if (!root) continue;
                var machines = root
                    .GetComponentsInChildren<ScriptMachine>()
                    .Where(x => x.isActiveAndEnabled && x.graph != null);
                scriptMachines.AddRange(machines);
            }
            AfterSceneExport(exporter, gltfRoot, scriptMachines);
        }

        private void OutputUnitLogs()
        {
            if (UnitExportLogging.unitLogMessages.Count == 0)
            {
                Debug.Log("Successfull Exported without any warnings!");
                return;
            }

            var sb = new StringBuilder();
            
            foreach (var unitLog in UnitExportLogging.unitLogMessages)
            {
                sb.AppendLine("Unit: "+unitLog.Key.ToString());
                foreach (var info in unitLog.Value.infos)
                    sb.AppendLine("   Info: "+info);
                foreach (var warning in unitLog.Value.warnings)
                    sb.AppendLine("   Warning: "+warning);
                foreach (var error in unitLog.Value.errors)
                    sb.AppendLine("   Error: "+error);
            }
            
            Debug.LogWarning("Exported with warnings/errors: "+System.Environment.NewLine+ sb.ToString());
        }
        
        private void PostIndexTopologicalSort()
        {
            // Resort the nodes after resolving the connections
            var sorted = PostTopologicalSort();
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
                
                foreach (var valueSocket in node.ValueSocketConnectionData)
                {
                    if (valueSocket.Value.Node != null && valueSocket.Value.Node.HasValue)
                        valueSocket.Value.Node = newIndices[valueSocket.Value.Node.Value];
                }
                foreach (var flowSocket in node.FlowSocketConnectionData)
                {
                    if (flowSocket.Value.Node != null && flowSocket.Value.Node.HasValue)
                        flowSocket.Value.Node = newIndices[flowSocket.Value.Node.Value];
                }
            }

            // Resort nodesToSerialize
            nodesToSerialize.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        private GltfTypes.TypeMapping[] CollectAndFilterUsedTypes()
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
                foreach (var config in node.ConfigurationData)
                {
                    if (config.Key == "type" && config.Value.Value != null)
                    {
                        if ((int)config.Value.Value != -1)
                            usedTypeIndices.Add((int)config.Value.Value);
                        
                    }
                }
                foreach (var valueSocket in node.ValueSocketConnectionData)
                    if (valueSocket.Value.Value != null)
                    {
                        if (valueSocket.Value.Type == -1)
                            valueSocket.Value.Type = GltfTypes.TypeIndex(valueSocket.Value.GetType());
                        usedTypeIndices.Add(valueSocket.Value.Type);
                    }
                
                foreach (var outSocket in node.OutValueSocket)
                    if (outSocket.Value.expectedType != null)
                    {
                        if (outSocket.Value.expectedType.typeIndex != null)
                            usedTypeIndices.Add(outSocket.Value.expectedType.typeIndex.Value);
                    }
            }
            
            // Create used Type Mapping List and mark the new indices
            foreach (var typeIndex in usedTypeIndices.OrderBy( t => t))
            {
                types.Add(GltfTypes.TypesMapping[typeIndex]);
                typesIndexReplacement.Add(typeIndex, types.Count-1);
            }
            
            // Replace the old type indices with the new ones
            foreach (var node in nodesToSerialize)
            {
                foreach (var config in node.ConfigurationData)
                {
                    if (config.Key == "type" && config.Value.Value != null)
                    {
                        if ((int)config.Value.Value != -1)
                            config.Value.Value = typesIndexReplacement[(int)config.Value.Value];
                    }
                }
                foreach (var valueSocket in node.ValueSocketConnectionData)
                    if (valueSocket.Value.Value != null && valueSocket.Value.Type != -1)
                        valueSocket.Value.Type = typesIndexReplacement[valueSocket.Value.Type];
            }
            
            foreach (var variable in variables.Where( v => v.Type != -1))
                variable.Type = typesIndexReplacement[variable.Type];
            
            foreach (var customEventValue in customEvents.SelectMany(c => c.Values))
                customEventValue.Value.Type = typesIndexReplacement[customEventValue.Value.Type];

            foreach (var declaration in opDeclarations.Where(d => d.inputValueSockets != null).SelectMany(d => d.inputValueSockets.Values)
                         .Concat(opDeclarations.Where(d => d.outputValueSockets != null).SelectMany(d => d.outputValueSockets.Values)))
                declaration.type = typesIndexReplacement[declaration.type];
            
            return types.ToArray();
        }
        
        private static LinkedList<IUnit> TopologicalSort(IEnumerable<IUnit> nodes)
        {
            var sorted = new LinkedList<IUnit>();
            var visited = new Dictionary<IUnit, bool>();

            void Visit(IUnit node)
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
                    HashSet<IUnit> dependencies = new HashSet<IUnit>();
                    foreach (IUnitConnection connection in node.connections)
                    {
                        if (connection.source.unit != node)
                        {
                            dependencies.Add(connection.source.unit);
                        }
                    }

                    foreach (IUnit dependency in dependencies)
                    {
                        Visit(dependency);
                    }

                    visited[node] = false;
                    sorted.AddLast(node);
                }
            }
            
            foreach (var node in nodes)
            {
                Visit(node);
            }

            return sorted;
        }

        private LinkedList<int> PostTopologicalSort()
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
                    foreach (var connection in currentNode.ValueSocketConnectionData)
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

        private void CheckForCircularFlows()
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
                    foreach (var connection in currentNode.FlowSocketConnectionData)
                    {
                        if (connection.Value.Node != null && connection.Value.Node.HasValue && connection.Value.Node.Value < nodesToSerialize.Count)
                        {
                            if (Visit(connection.Value.Node.Value))
                            {
                                // Add Events because of cyclic dependency
                                var eventId = AddEventWithIdIfNeeded($"CyclicDependency{connection.Value.Node.ToString()}from{node.ToString()}");
                       
                                var triggerEventNode = new GltfInteractivityNode(new Event_SendNode());
                                triggerEventNode.Index = nodesToSerialize.Count;
                                triggerEventNode.ConfigurationData["event"].Value = eventId;
                                nodesToSerialize.Add(triggerEventNode);
                                
                                var receiveEventNode = new GltfInteractivityNode(new Event_ReceiveNode());
                                receiveEventNode.Index = nodesToSerialize.Count;
                                receiveEventNode.ConfigurationData["event"].Value = eventId;
                                nodesToSerialize.Add(receiveEventNode);

                                var receiveFlowOut = receiveEventNode.FlowSocketConnectionData[Event_ReceiveNode.IdFlowOut];
                                receiveFlowOut.Node = connection.Value.Node;
                                receiveFlowOut.Socket = connection.Value.Socket;    

                                connection.Value.Node = triggerEventNode.Index;
                                connection.Value.Socket = Event_SendNode.IdFlowIn;
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

        internal void RemoveNode(GltfInteractivityNode nodeToRemove)
        {
            var indexToRemove = nodesToSerialize.IndexOf(nodeToRemove);
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
                foreach (var valueSocket in n.ValueSocketConnectionData)
                {
                    if (valueSocket.Value.Node == indexToReplace)
                        valueSocket.Value.Node = nodeToRemove.Index;
                }
                foreach (var flowSocket in n.FlowSocketConnectionData)
                {
                    if (flowSocket.Value.Node == indexToReplace)
                        flowSocket.Value.Node = nodeToRemove.Index;
                }
            }
        }
        
        private void RemoveNodes(IReadOnlyList<GltfInteractivityNode> nodesToRemove)
        {
            foreach (var removedNode in nodesToRemove)
            {
                RemoveNode(removedNode);
            }
        }

        private void CleanUp()
        {
            var nodeCountBefore = nodesToSerialize.Count;
            bool hasChanges = false;
            do
            {
                hasChanges = CleanUpRegistry.StartCleanUp(this);
            } while (hasChanges);
            
            var nodeCountAfter = nodesToSerialize.Count;
            if (nodeCountBefore != nodeCountAfter)
            {
                Debug.Log($"Removed {nodeCountBefore - nodeCountAfter} nodes in cleanup.");
            }
        }

        private void RemoveUnconnectedNodes()
        {
            var visited = new HashSet<int>(nodesToSerialize.Count);
            var nodesToRemove = new List<GltfInteractivityNode>();
           
            //Collect which nodes has connections
            foreach (var node in nodesToSerialize)
            {
                if (node.ValueSocketConnectionData.Count > 0)
                {
                    foreach (var valueSocket in node.ValueSocketConnectionData)
                    {
                        if (valueSocket.Value.Node != null && valueSocket.Value.Node != -1)
                        {
                            visited.Add(valueSocket.Value.Node.Value);
                            visited.Add(node.Index);
                        }
                    }
                }

                if (node.FlowSocketConnectionData.Count > 0)
                {
                    foreach (var flowSocket in node.FlowSocketConnectionData)
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
        
        private GltfInteractivityNode[] AddTypeConversion(GltfInteractivityNode targetNode, int conversionNodeIndex, string targetInputSocket, int fromType, int toType)
        {
            List<GltfInteractivityNode> newNodes = new List<GltfInteractivityNode>();
            
            var fromTypeSignature = GltfTypes.TypesMapping[fromType].GltfSignature;
            var toTypeSignature = GltfTypes.TypesMapping[toType].GltfSignature;

            var targetSocketData = targetNode.ValueSocketConnectionData[targetInputSocket];
            GltfInteractivityNode conversionNode = null;
            
            var fromTypeComponentCount = GltfTypes.GetComponentCount(fromTypeSignature);
            var toTypeComponentCount = GltfTypes.GetComponentCount(toTypeSignature);
            
            void SetupSimpleConversion(GltfInteractivityNodeSchema schema)
            {
                conversionNode= new GltfInteractivityNode(schema);
                conversionNode.Index = conversionNodeIndex;
                newNodes.Add(conversionNode);
                conversionNode.ValueSocketConnectionData["a"] = new GltfInteractivityNode.ValueSocketData()
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
                conversionNode= new GltfInteractivityNode(schema);
                conversionNode.Index = conversionNodeIndex;
                newNodes.Add(conversionNode);

                int indexForTargetNodeInput = conversionNodeIndex;
                
                if (fromTypeComponentCount == 1 && toTypeComponentCount > 1)
                {
                    foreach (var input in conversionNode.ValueSocketConnectionData)
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
                    }
                    
                    var extractNode = new GltfInteractivityNode(extractSchema);
                    extractNode.Index = conversionNodeIndex + newNodes.Count;
                    newNodes.Add(extractNode);
                    
                    var extractInput = extractNode.ValueSocketConnectionData["a"];
                    extractInput.Node = targetSocketData.Node;
                    extractInput.Socket = targetSocketData.Socket;
                    extractInput.Value = targetSocketData.Value;
                    extractInput.Type = targetSocketData.Type;

                    int inputExtractIndex = 0;
                    foreach (var inputSocket in conversionNode.ValueSocketConnectionData)
                    {
                        if (inputExtractIndex <= extractNode.OutValueSocket.Count - 1)
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

        private void CheckForImplicitValueConversions()
        {
            foreach (var node in nodesToSerialize.ToArray())
            {
                foreach (var valueSocket in node.ValueSocketConnectionData)
                {
                    var socket = valueSocket.Value;
              
                    if (valueSocket.Value.Node == null && valueSocket.Value.Value == null)
                    {
                        // Try to handle nulls

                        if (socket.typeRestriction != null)
                        {
                            if (socket.typeRestriction.limitToType != null)
                            {
                                valueSocket.Value.Value = GltfTypes.GetNullByType(socket.typeRestriction.limitToType);
                                valueSocket.Value.Type = GltfTypes.TypeIndexByGltfSignature(socket.typeRestriction.limitToType);
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
                                var conversionNode = AddTypeConversion(node, nodesToSerialize.Count, valueSocket.Key,
                                    valueType, limitToType);
                                if (conversionNode != null)
                                    nodesToSerialize.AddRange(conversionNode);
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
                                if (preferType == -1)
                                {
                                    continue;
                                }
                                var conversionNode = AddTypeConversion(node, nodesToSerialize.Count, valueSocket.Key,
                                    valueType, preferType);
                                if (conversionNode != null)
                                    nodesToSerialize.AddRange(conversionNode);
                            }
                        }
                    }
                }
            }

        }


        private void CollectOpDeclarations()
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
                        foreach (var input in node.ValueSocketConnectionData)
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
                        
                        foreach (var output in node.OutValueSocket)
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

        private void ValidateData()
        {
            var sb = new StringBuilder();
            
            void NodeAppendLine(GltfInteractivityNode node, string message)
            {
                if (node.ConfigurationData != null)
                {
                    foreach (var config in node.ConfigurationData)
                        message += $" (config: {config.Key}={config.Value.Value})";
                }
                
                foreach (var g in addedGraphs)
                    foreach (var kvpNode in g.nodes)
                        if (kvpNode.Value.Nodes.Contains(node))
                        {
                            UnitExportLogging.AddErrorLog(kvpNode.Key, "Validation Error: "+message);
                            sb.AppendLine($"Node Index {node.Index} with Schema={node.Schema.Op} from Unit={kvpNode.Key}: {message}");
                            return;
                        }
                
                sb.AppendLine($"Node Index {node.Index} with Schema={node.Schema.Op}: {message}");
            }
            
            foreach (var node in nodesToSerialize)
            {
                foreach (var config in node.ConfigurationData)
                {
                    if (config.Value.Value == null)
                    {
                        NodeAppendLine(node, $"Configuration with <{config.Key}> has no Value");
                    }
                }
                
                foreach (var valueSocket in node.ValueSocketConnectionData)
                {
                    if (valueSocket.Value.Node == null)
                    {
                        if (valueSocket.Value.Value == null)
                            NodeAppendLine(node, $"Socket {valueSocket.Key} has no connection and no Value");
                        else if (valueSocket.Value.Type == -1)
                            NodeAppendLine(node, $"Socket {valueSocket.Key} has invalid Type (-1). Value-Type: {valueSocket.Value.Value.GetType().Name}");
                    }
                    else if (valueSocket.Value.Node == -1)
                    {
                        NodeAppendLine(node, $"Socket {valueSocket.Key} has invalid Node Index (-1)");
                    }
                }
                
                foreach (var flowSocket in node.FlowSocketConnectionData)
                {
                    if (flowSocket.Value.Node == -1)
                    {
                        NodeAppendLine(node, $"Flow Socket {flowSocket.Key} has invalid Node Index (-1)");
                    }
                }

                if (node.Schema.Op == "pointer/set" || node.Schema.Op == "pointer/get")
                {
                    if (node.ValueSocketConnectionData.TryGetValue(UnitsHelper.IdPointerNodeIndex, out var valueSocket))
                    {
                        if (valueSocket.Value != null && valueSocket.Node == null && (int)valueSocket.Value == -1)
                            NodeAppendLine(node, $"Node Pointer Node has invalid nodeIndex Value: -1");
                    }
                }
                
                if (node.Schema.Op == "variable/set" || node.Schema.Op == "variable/get")
                {
                    if (node.ConfigurationData.TryGetValue(Variable_SetNode.IdConfigVarIndex, out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Variable Node has no Variable Index");
                        if (varConfig.Value != null && (int)varConfig.Value == -1)
                            NodeAppendLine(node, $"Variable Node has invalid Variable Index: -1");
                    }
                }

                if (node.Schema.Op == "event/receive" || node.Schema.Op == "event/send")
                {
                    if (node.ConfigurationData.TryGetValue("event", out var varConfig))
                    {
                        if (varConfig.Value == null)
                            NodeAppendLine(node, $"Event Node has no Event Index");
                        if (varConfig.Value != null && (int)varConfig.Value == -1)
                            NodeAppendLine(node, $"Event Node has invalid Event Index: -1");
                    }
                }
            }
            
            foreach (var variable in variables)
            {
                if (variable.Type == -1)
                    sb.AppendLine($"Variable with Id >{variable.Id}< has invalid Type (-1)");
            }
            
            foreach (var customEvent in customEvents)
            {
                foreach (var customEventValue in customEvent.Values)
                {
                    if (customEventValue.Value.Type == -1)
                        sb.AppendLine($"Custom Event with Id >{customEvent.Id}< with Value >{customEventValue.Key}< has invalid Value Type (-1)");
                }
            }
            
            if (sb.Length == 0)
                return;
            
            Debug.LogError($"Validation Errors Found: "+ System.Environment.NewLine + sb.ToString());
        }
    }
}
