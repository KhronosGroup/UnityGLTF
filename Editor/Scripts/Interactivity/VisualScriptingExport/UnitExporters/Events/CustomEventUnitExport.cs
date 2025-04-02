using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class CustomEventUnitExport: IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new CustomEventUnitExport());
        }
        
        public System.Type unitType { get => typeof(CustomEvent); }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as CustomEvent;

            if (!unit.target.hasDefaultValue && !unit.target.hasValidConnection)
            {
                UnitExportLogging.AddErrorLog(unit, "Could not find target node for CustomEvent");
                return false;
            }
            
            var node = unitExporter.CreateNode(new Event_ReceiveNode());
            
            var args = new Dictionary<string, GltfInteractivityUnitExporterNode.EventValues>();
            args.Add("targetNodeIndex", new GltfInteractivityUnitExporterNode.EventValues {Type = GltfTypes.TypeIndexByGltfSignature("int") });

            foreach (var arg in unit.argumentPorts)
            {
                var argId = arg.key;
                var argTypeIndex = GltfTypes.TypeIndex(arg.type);
                var newArg = new GltfInteractivityUnitExporterNode.EventValues { Type = argTypeIndex };
                args.Add(argId, newArg);
                // TODO: adding default values?
                node.ValueOut(argId).MapToPort(arg);
            }
            
            var index = unitExporter.exportContext.AddEventIfNeeded(unit, args);
            node.Configuration["event"].Value = index;
            node.ValueOut("targetNodeIndex").ExpectedType(ExpectedType.Int);

            // Setup target Node checks
            var eqIdNode = unitExporter.CreateNode(new Math_EqNode());
            eqIdNode.ValueIn(Math_EqNode.IdValueA).ConnectToSource(node.ValueOut("targetNodeIndex")).SetType(TypeRestriction.LimitToInt);
            var currentGameObject = unitExporter.exportContext.currentGraphProcessing.gameObject;
            var transformIndex = unitExporter.exportContext.exporter.GetTransformIndex(currentGameObject.transform);
            eqIdNode.ValueIn(Math_EqNode.IdValueB).SetValue(transformIndex).SetType(TypeRestriction.LimitToInt);            
            
            var branchNode = unitExporter.CreateNode(new Flow_BranchNode());
            branchNode.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(eqIdNode.FirstValueOut());
            node.FlowOut(Event_ReceiveNode.IdFlowOut)
                .ConnectToFlowDestination(branchNode.FlowIn(Flow_BranchNode.IdFlowIn));

            branchNode.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.trigger);
            
            void ResolveTypes()
            {
                var customEvent = unitExporter.exportContext.customEvents[index];

                foreach (var argValue in args)
                {
                    var eventValue = customEvent.Values.FirstOrDefault(x => x.Key == argValue.Key);
                    if (eventValue.Value == null)
                        continue;

                    int argTypeIndex = -1;
                    if (eventValue.Value.Type == -1)
                        argTypeIndex = unitExporter.exportContext.GetValueTypeForOutput(node, argValue.Key);
                    else
                        argTypeIndex = eventValue.Value.Type;
                    
                    eventValue.Value.Type = argTypeIndex;
                    if (argTypeIndex == -1) 
                        UnitExportLogging.AddErrorLog(unit, "Could not resolve type for event value: "+argValue.Key);
                    else
                        node.ValueOut(argValue.Key).ExpectedType(ExpectedType.GtlfType(argTypeIndex));
                }
            };
  
            unitExporter.exportContext.OnNodesCreated += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };

            unitExporter.exportContext.OnBeforeSerialization += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };   
            return true;
        }
    }
}