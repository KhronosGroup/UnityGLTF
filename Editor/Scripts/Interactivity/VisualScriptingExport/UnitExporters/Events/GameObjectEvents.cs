using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public abstract class GameObjectEvents<TVisualGraphUnit, TNodeSchema> : IUnitExporter 
        where TNodeSchema : GltfInteractivityNodeSchema, new()
        where TVisualGraphUnit : class, IGameObjectEventUnit
    {
        public Type unitType
        {
            get => typeof(TVisualGraphUnit);
        }

        protected virtual string NodeIndexId => Event_OnSelectNode.IdValueSelectedNodeIndex;
        protected virtual string HitLocationId => Event_OnSelectNode.IdValueLocalHitLocation;
        protected virtual string ControllerIndexId => Event_OnSelectNode.IdValueControllerIndex;

        protected virtual void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as TVisualGraphUnit;
            var node = unitExporter.CreateNode<TNodeSchema>();

            if (!unit.valueInputs.TryGetValue("target", out var targetInput))
            {
                UnitExportLogging.AddErrorLog(unit, "Could not find target node for CustomEvent");
                return false;
            }

            if (!unit.controlOutputs.TryGetValue("trigger", out var triggerOutput))
            {
                return false;
            }
            
            // NodeIndex's value should equal the ID of the object referenced by the target value input.
            GameObject target = UnitsHelper.GetGameObjectFromValueInput(targetInput, unit.defaultValues, unitExporter.vsExportContext);

            if (target == null)
            {
                UnitExportLogging.AddErrorLog(unit, "No target object found.");
                return false;
            }

            int targetIndex = unitExporter.vsExportContext.exporter.GetTransformIndex(target.transform);
            node.Configuration["nodeIndex"].Value = targetIndex;
            OnTargetNodeConfigured(unitExporter, targetIndex);
            
            // Config for stop propagation will just default to False. We don't have an equivalent
            // parameter in Unity Visual Scripting so we will default to preventing the selection
            // event from being propagated up the hierarchy.
            node.Configuration["stopPropagation"].Value = false;

            node.FlowOut("out").MapToControlOutput(triggerOutput);
            
            // Resolve PointerEventData in out connections
            
            foreach (var vo in unit.valueOutputs)
            {
                if (!vo.hasValidConnection)
                    continue;
                
                foreach (var connection in vo.connections)
                {
                    var targetUnit = connection.destination.unit;
                    if (targetUnit is GetMember getMember)
                    {
                        var memberStr = getMember.member.ToString();
                        if (memberStr == "PointerEventData.pointerEnter"
                            || memberStr == "PointerEventData.pointerClick")
                        {
                            if (node.OutputValueSocket.ContainsKey(NodeIndexId))
                                node.ValueOut(NodeIndexId).MapToPort(getMember.value);
                        }
                        // else if (memberStr == "PointerEventData.position")
                        // {
                        //     exportNode.AddValueOutportSocketName(getMember.value,
                        //         GltfInt_OnSelectNode.IdValueLocalHitLocation, node);
                        // }
                        else if (memberStr == "PointerEventData.pointerId")
                        {
                            if (node.OutputValueSocket.ContainsKey(ControllerIndexId))
                                unitExporter.MapValueOutportToSocketName(getMember.value, ControllerIndexId, node);
                        }
                        else if (memberStr == "PointerEventData.pointerId")
                        {
                            if (node.OutputValueSocket.ContainsKey(ControllerIndexId))
                                unitExporter.MapValueOutportToSocketName(getMember.value, ControllerIndexId, node);
                        }
                        else if (memberStr == "PointerEventData.pointerCurrentRaycast")
                        {
                           foreach (var getMemberValueOut in getMember.valueOutputs)
                            foreach (var getMemberConnection in getMemberValueOut.connections)
                            {
                                var secondTargetUnit = getMemberConnection.destination.unit;
                                if (secondTargetUnit is GetMember secondGetMember)
                                {
                                    if (secondGetMember.member.ToString() == "RaycastResult.worldPosition")
                                    {
                                        if (node.OutputValueSocket.ContainsKey(HitLocationId))
                                            unitExporter.MapValueOutportToSocketName(secondGetMember.value, HitLocationId, node);
                                    }
                                    if (secondGetMember.member.ToString() == "RaycastResult.gameObject")
                                    {
                                        if (node.OutputValueSocket.ContainsKey(NodeIndexId))
                                            unitExporter.MapValueOutportToSocketName(secondGetMember.value, NodeIndexId, node);
                                    }
                                    
                                }
                            }
                        }
                    }
                    else
                    if (targetUnit is Expose expose && expose.type == typeof(PointerEventData))
                    {
                        if (expose.valueOutputs.TryGetValue("pointerEnter", out var pointerEnterOutput)
                            && node.OutputValueSocket.ContainsKey(NodeIndexId))
                            unitExporter.MapValueOutportToSocketName(pointerEnterOutput, NodeIndexId, node);

                        if (expose.valueOutputs.TryGetValue("position", out var pointerPositionOutput)
                            && node.OutputValueSocket.ContainsKey(HitLocationId))
                            unitExporter.MapValueOutportToSocketName(pointerPositionOutput, HitLocationId, node);
                        
                        if (expose.valueOutputs.TryGetValue("pointerId", out var pointerIdOutput)
                            && node.OutputValueSocket.ContainsKey(ControllerIndexId))
                            unitExporter.MapValueOutportToSocketName(pointerIdOutput, ControllerIndexId, node);
                    }
                }
            
            }
            return true;
        }
    }
}