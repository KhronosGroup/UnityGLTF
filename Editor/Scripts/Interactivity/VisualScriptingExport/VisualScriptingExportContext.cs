//#define INTERACTIVITY_DEBUG_LOGS

using System.Text;
using UnityEngine.SceneManagement;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using UnityEngine;
    using Unity.VisualScripting;
    using UnityGLTF;
    
    public class VisualScriptingExportContext: InteractivityExportContext
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
            public SubgraphUnit subGraphUnit = null;
        }
        #endregion
        
        public ScriptMachine ActiveScriptMachine = null;
        
        private List<UnitExporter> allUnitExporters = new List<UnitExporter>();
        
        public delegate void OnUnitNodesCreatedDelegate(List<GltfInteractivityExportNode> nodes);
        public event OnUnitNodesCreatedDelegate OnUnitNodesCreated;
        
        internal Dictionary<InputPortGraph, InputPortGraph> graphBypasses = new Dictionary<InputPortGraph, InputPortGraph>(new InputportGraphComparer());
        internal List<ExportGraph> addedGraphs = new List<ExportGraph>();
        private List<VariableBasedList> addedVariableBasedLists = new List<VariableBasedList>();
        private List<Scene> _scenes = new List<Scene>();
        
        internal ExportGraph currentGraphProcessing { get; private set; } = null;
        
        public bool cleanUpAndOptimizeExportedGraph = true;
        
        public VisualScriptingExportContext() 
        {
        }
        
        private Scene GetCurrentScene()
        {
#if UNITY_2022_3_OR_NEWER
            return GameObject.GetScene(currentGraphProcessing.gameObject.GetInstanceID());
#else
            return SceneManager.GetActiveScene();
#endif
        }
        
        private int GetSceneIndex(Scene scene)
        {
            if (!_scenes.Contains(scene))
                _scenes.Add(scene);
            
            return _scenes.IndexOf(scene);
        }

        public VariableBasedListFromUnit GetListByCreator(IUnit listCreatorUnit)
        {
            return addedVariableBasedLists
                .Where( v => v is VariableBasedListFromUnit)
                .Cast<VariableBasedListFromUnit>()
                .FirstOrDefault(l => l.listCreatorUnit == listCreatorUnit && l.listCreatorGraph == currentGraphProcessing);
        }
        
        public VariableBasedList GetListByName(string listId)
        {
            return addedVariableBasedLists.FirstOrDefault(l => l.ListId == listId);
        }
        
        public VariableBasedListFromGraphVariable GetListByCreator(VariableDeclaration variable)
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
                    varDeclarations = Variables.Scene(GetCurrentScene());
                    if (varDeclarations == null)
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
                    // Get scene from where the GameObject lives
                    var scene = GetCurrentScene();
                    var sceneIndex = GetSceneIndex(scene);
                    exportVarName = $"scene{sceneIndex}_{varName}";
                    varDeclarations = Variables.Scene(scene);
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
                    return null;
                }
            }
            else
            {
                UnitExportLogging.AddErrorLog(unit, "Variable not found");
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
                    return null;
                }
            }
            
            return varValue;
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

            return AddVariableWithIdIfNeeded(id, defaultValue, gltfTypeIndex);
        }
        
        public int AddEventIfNeeded(Unit eventUnit, Dictionary<string, GltfInteractivityNode.EventValues> arguments = null)
        {
            var eventId = eventUnit.defaultValues["name"] as string;
            if (string.IsNullOrEmpty(eventId))
            {
                UnitExportLogging.AddErrorLog(eventUnit, "No event selected.");
                return -1;
            }
            
            return AddEventWithIdIfNeeded(eventId, arguments);
        }
        
        internal ExportGraph AddGraph(FlowGraph graph, SubgraphUnit subGraphUnit = null)
        {
            var newExportGraph = new ExportGraph();

            newExportGraph.subGraphUnit = subGraphUnit;
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
            // so other nodes which are required existing List/Array creators can find them
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
            
            allUnitExporters.AddRange(newExportGraph.nodes.Select( g => g.Value));
            currentGraphProcessing = lastCurrentGraph;
            return newExportGraph;
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
    
            foreach (var scriptMachine in visualScriptingComponents)
            {
                ActiveScriptMachine = scriptMachine;
                FlowGraph flowGraph = scriptMachine.graph;
                AddGraph(flowGraph);
            }
            
            nodesToSerialize = allUnitExporters.SelectMany(exportNode => exportNode.Nodes).Cast<GltfInteractivityExportNode>().ToList();

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
            
            OnUnitNodesCreated?.Invoke(nodesToSerialize);
            
            RemoveUnconnectedNodes();

            TriggerInterfaceExportCallbacks();
            
            // For Value Conversion, we need to presort the nodes, otherwise we might get wrong results
            TopologicalSort();
            CheckForImplicitValueConversions();
            
            CheckForCircularFlows();
            
            if (cleanUpAndOptimizeExportedGraph)
                CleanUp();
            
            // Final Topological Sort
            TopologicalSort();  
            
            CollectOpDeclarations();
            
            TriggerOnBeforeSerialization();
            
            ApplyInteractivityExtension();
            OutputUnitLogs();
        }
        
        /// <summary>
        /// Called after the scene has been exported to add interactivity data.
        /// </summary>
        /// <param name="exporter"> GLTFSceneExporter object used to export the scene</param>
        /// <param name="gltfRoot"> Root GLTF object for the gltf object tree</param>
        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            this.exporter = exporter;
            ActiveGltfRoot = gltfRoot;
            
            UnitExportLogging.ClearLogs();

            var scriptMachines = new List<ScriptMachine>();

            var warningLog = new StringBuilder();
            
            string GetHierarchyName(Transform transform)
            {
                if (transform == null)
                    return "";
                return GetHierarchyName(transform.parent) + "/" + transform.name;
            }
            
            foreach (var root in exporter.RootTransforms)
            {
                if (!root) continue;
                var machines = root
                    .GetComponentsInChildren<ScriptMachine>(true)
                    .Where(x => x.graph != null);
                
                scriptMachines.AddRange(machines.Where(m => m.isActiveAndEnabled));
                
                // Just for warning log, we collect the inactive machines, which will be ignored for export
                var inactiveMachines = machines.Where(m => !m.isActiveAndEnabled);
                foreach (var inactive in inactiveMachines)
                    warningLog.AppendLine(GetHierarchyName(inactive.transform));
            }

            if (warningLog.Length > 0)
            {
                warningLog.Insert(0, "Inactive Script Machines found! Following Script Machine will be ignored for export: \n");
                Debug.LogWarning(warningLog.ToString());
            }
            
            AfterSceneExport(exporter, gltfRoot, scriptMachines);
        }

        private void OutputUnitLogs()
        {
            if (UnitExportLogging.unitLogMessages.Count == 0)
                return;
    
            var sb = new StringBuilder();
            
            foreach (var unitLog in UnitExportLogging.unitLogMessages)
            {
                sb.AppendLine("Unit: "+ UnitsHelper.UnitToString(unitLog.Key));
                foreach (var info in unitLog.Value.infos.Distinct())
                    sb.AppendLine("   Info: "+info);
                foreach (var warning in unitLog.Value.warnings.Distinct())
                    sb.AppendLine("   Warning: "+warning);
                foreach (var error in unitLog.Value.errors.Distinct())
                    sb.AppendLine("   Error: "+error);
            }
            
            Debug.LogWarning("Exported with warnings/errors: "+System.Environment.NewLine+ sb.ToString());
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
        
        
     
    }
}
