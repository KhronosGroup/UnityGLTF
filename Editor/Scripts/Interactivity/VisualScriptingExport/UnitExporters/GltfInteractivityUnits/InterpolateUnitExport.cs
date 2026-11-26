using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export

{
    public class InterpolateUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InterpolateMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new InterpolateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InterpolateMember;

            string pointerTemplate = null;
            string pointerId = null;
            ValueInRef originalValue = null;
            ValueOutRef convertedValue = null;

            var valueType = GltfTypes.Float;
            if (unit.member.targetType == typeof(Transform))
            {
                pointerId = PointersHelper.IdPointerNodeRef;
                var spaceConversion = unitExporter.Context.addUnityGltfSpaceConversion;
                var memberName = unit.member.name;
                // A root node in the glTF has the same world and local transform, so the world<>local conversion isn't needed
                if (TransformHelpersVS.IsExportedRoot(unitExporter, unit.target))
                {
                    if (memberName == nameof(Transform.position))
                        memberName = nameof(Transform.localPosition);
                    else if (memberName == nameof(Transform.rotation))
                        memberName = nameof(Transform.localRotation);
                    else if (memberName == nameof(Transform.lossyScale))
                        memberName = nameof(Transform.localScale);
                }

                if (memberName == nameof(Transform.localPosition))
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/translation";
                    valueType = GltfTypes.Float3;
                    if (spaceConversion)
                    {
                        SpaceConversionHelpers.AddSpaceConversion(unitExporter, out originalValue, out convertedValue);
                        originalValue.MapToInputPort(unit.input);
                    }
                }
                else if (memberName == nameof(Transform.position))
                {
                    // Glb only has local transforms: the world target value is converted once to the
                    // local target value (with the parent's transform at the start), which is then interpolated
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/translation";
                    valueType = GltfTypes.Float3;
                    TransformHelpers.GetLocalPositionFromWorldPosition(unitExporter, out var targetInput, out originalValue, out convertedValue);
                    targetInput.MapToInputPort(unit.target);
                    originalValue.MapToInputPort(unit.input);
                }
                else if (memberName == nameof(Transform.localRotation))
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/rotation";
                    valueType = GltfTypes.Float4;
                    if (spaceConversion)
                    {
                        SpaceConversionHelpers.AddRotationSpaceConversion(unitExporter, out originalValue, out convertedValue);
                        originalValue.MapToInputPort(unit.input);
                    }
                }
                else if (memberName == nameof(Transform.rotation))
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/rotation";
                    valueType = GltfTypes.Float4;
                    TransformHelpers.GetLocalRotationFromWorldRotation(unitExporter, out var targetInput, out originalValue, out convertedValue);
                    targetInput.MapToInputPort(unit.target);
                    originalValue.MapToInputPort(unit.input);
                }
                else if (memberName == nameof(Transform.localScale))
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/scale";
                    valueType = GltfTypes.Float3;
                }
                else if (memberName == nameof(Transform.lossyScale))
                {
                    // localScale = worldScale * currentLocalScale / currentWorldScale (= worldScale / parentWorldScale)
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "/scale";
                    valueType = GltfTypes.Float3;
                    TransformHelpersVS.GetLocalScale(unitExporter, unit.target, out var currentLocalScale);
                    TransformHelpersVS.GetWorldScale(unitExporter, unit.target, out var currentWorldScale);

                    var mul = unitExporter.CreateNode<Math_MulNode>();
                    originalValue = mul.ValueIn(Math_MulNode.IdValueA);
                    originalValue.MapToInputPort(unit.input);
                    mul.ValueIn(Math_MulNode.IdValueB).ConnectToSource(currentLocalScale);

                    var div = unitExporter.CreateNode<Math_DivNode>();
                    div.ValueIn(Math_DivNode.IdValueA).ConnectToSource(mul.FirstValueOut());
                    div.ValueIn(Math_DivNode.IdValueB).ConnectToSource(currentWorldScale);
                    convertedValue = div.FirstValueOut();
                }
            }

            if (unit.member.targetType == typeof(Material))
            {
                var materialTemplate = PointersHelper.IdPointerTemplMaterialByRef;
                pointerId = PointersHelper.IdPointerMaterialRef;
                if (unit.member.name == "color")
                {
                     var gltfProperty =
                     MaterialPointerHelper.GetPointer(unitExporter, "_Color", out var map);
                    if (gltfProperty == null)
                    {
                        UnitExportLogging.AddErrorLog(unit, "color property name is not supported.");
                        return false;
                    }

                    valueType = GltfTypes.Float4;
                    pointerTemplate = materialTemplate + "/" + gltfProperty;
                }
            }
            
            if (string.IsNullOrEmpty(pointerTemplate))
            { 
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target type for InterpolateMember. Maybe it's not supported.");
                return false;
            }
            
            var node = unitExporter.CreateNode<Pointer_InterpolateNode>();

            node.FlowIn(Pointer_InterpolateNode.IdFlowIn).MapToControlInput(unit.assign);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOut).MapToControlOutput(unit.assigned);
            
            if (convertedValue == null)
                node.ValueIn(Pointer_InterpolateNode.IdValue).MapToInputPort(unit.input);
            else
                node.ValueIn(Pointer_InterpolateNode.IdValue).ConnectToSource(convertedValue);
            
            node.ValueIn(Pointer_InterpolateNode.IdDuration).MapToInputPort(unit.duration);
            node.ValueIn(Pointer_InterpolateNode.IdPoint1).MapToInputPort(unit.pointA);
            node.ValueIn(Pointer_InterpolateNode.IdPoint2).MapToInputPort(unit.pointB);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOutDone).MapToControlOutput(unit.done);
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, pointerId, unit.target, pointerTemplate, valueType);
            return true;
        }
    }
}