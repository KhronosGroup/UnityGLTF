using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class TriggerCustomEventUnitExport: IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new TriggerCustomEventUnitExport());
        }
        
        public System.Type unitType { get => typeof(TriggerCustomEvent); }
        

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as TriggerCustomEvent;
            if (!unit.target.hasDefaultValue && !unit.target.hasValidConnection)
            {
                UnitExportLogging.AddErrorLog(unit, "Could not find target node for CustomEvent");
                return false;
            }
            
            var node = unitExporter.CreateNode<Event_SendNode>();
            
            unitExporter.MapInputPortToSocketName(unit.name, Event_SendNode.IdEvent, node);
            unitExporter.MapInputPortToSocketName(unit.enter, Event_SendNode.IdFlowIn, node);
            
            node.ValueIn("targetNodeIndex").MapToInputPort(unit.target).SetType(TypeRestriction.LimitToInt);
            
            var args = new Dictionary<string, GltfInteractivityUnitExporterNode.EventValues>();
            args.Add("targetNodeIndex", new GltfInteractivityUnitExporterNode.EventValues { Type = GltfTypes.TypeIndexByGltfSignature("int")  });
            
            foreach (var arg in unit.arguments)
            {
                var argId = arg.key;
                var argTypeIndex = GltfTypes.TypeIndex(arg.type);
                var eventValue = new GltfInteractivityUnitExporterNode.EventValues { Type = argTypeIndex };
                args.Add(argId, eventValue);

                node.ValueIn(argId).MapToInputPort(arg);
            }
            var index = unitExporter.vsExportContext.AddEventIfNeeded(unit, args);

            void ResolveTypes()
            {
                var customEvent = unitExporter.vsExportContext.customEvents[index];

                foreach (var argValue in args)
                {
                    var eventValue = customEvent.Values.FirstOrDefault(x => x.Key == argValue.Key);
                    if (eventValue.Value == null)
                        continue;

                    int argTypeIndex = -1;
                    if (eventValue.Value.Type == -1)
                        argTypeIndex = unitExporter.vsExportContext.GetValueTypeForInput(node, argValue.Key);
                    else
                        argTypeIndex = eventValue.Value.Type;

                    eventValue.Value.Type = argTypeIndex;
                    if (argTypeIndex == -1)
                        UnitExportLogging.AddErrorLog(unit, "Could not resolve type for event value: " + argValue.Key);
                    else
                        node.ValueIn(argValue.Key).SetType(TypeRestriction.LimitToType(argTypeIndex));
                }
            }

            // Set the type of the event values on a later stage when we can identify the type of the input.
            // Also in case a Event Trigger uses a NULL as input, we also check for the input types for existing events
            unitExporter.vsExportContext.OnUnitNodesCreated += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };

            unitExporter.vsExportContext.OnBeforeSerialization += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };
            
            if (index == -1)
            {
                return false;
            }
            node.Configuration["event"].Value = index;
            
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Event_SendNode.IdFlowOut, node);
            return true;
        }
    }
}