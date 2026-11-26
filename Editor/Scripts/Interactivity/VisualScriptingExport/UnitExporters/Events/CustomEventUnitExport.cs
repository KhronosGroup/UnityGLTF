using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
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
            
            var node = unitExporter.CreateNode<Event_ReceiveNode>();
            
            var args = new Dictionary<string, GltfInteractivityUnitExporterNode.EventValues>();
            args.Add("targetNodeRef", new GltfInteractivityUnitExporterNode.EventValues {Type = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Ref) });

            foreach (var arg in unit.argumentPorts)
            {
                var argId = arg.key;
                var argTypeIndex = -1;
                var newArg = new GltfInteractivityUnitExporterNode.EventValues { Type = argTypeIndex };
                args.Add(argId, newArg);
                // TODO: adding default values?
                node.ValueOut(argId).MapToPort(arg);
            }
            
            var index = unitExporter.vsExportContext.AddEventIfNeeded(unit, args);
            node.Configuration["event"].Value = index;
            node.ValueOut("targetNodeRef").ExpectedType(ExpectedType.Ref);

            // Setup target Node checks
            var eqIdNode = unitExporter.CreateNode<Ref_EqNode>();
            eqIdNode.ValueIn(Ref_EqNode.IdValueA).ConnectToSource(node.ValueOut("targetNodeRef")).SetType(TypeRestriction.LimitToRef);
            var currentGameObject = unitExporter.vsExportContext.currentGraphProcessing.gameObject;
            eqIdNode.ValueIn(Ref_EqNode.IdValueB).MapToInputPort(unit.target);
            
            var branchNode = unitExporter.CreateNode<Flow_BranchNode>();
            branchNode.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(eqIdNode.FirstValueOut());
            node.FlowOut(Event_ReceiveNode.IdFlowOut)
                .ConnectToFlowDestination(branchNode.FlowIn(Flow_BranchNode.IdFlowIn));

            branchNode.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.trigger);
            
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
                        argTypeIndex = unitExporter.vsExportContext.GetValueTypeForOutput(node, argValue.Key);
                    else
                        argTypeIndex = eventValue.Value.Type;
                    
                    eventValue.Value.Type = argTypeIndex;
                    if (argTypeIndex == -1) 
                        UnitExportLogging.AddErrorLog(unit, "Could not resolve type for event value: "+argValue.Key);
                    else
                        node.ValueOut(argValue.Key).ExpectedType(ExpectedType.GtlfType(argTypeIndex));
                }
            };
  
            unitExporter.vsExportContext.OnUnitNodesCreated += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };

            unitExporter.vsExportContext.OnBeforeSerialization += (List<GltfInteractivityExportNode> nodes) =>
            {
                ResolveTypes();
            };   
            return true;
        }
    }
}