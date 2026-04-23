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
            if (unitExporter.Context.addUnityGltfSpaceConversion && unit.member.targetType == typeof(Transform))
            {
                pointerId = PointersHelper.IdPointerNodeRef;
                // TODO: transform space conversion for targetValue!!!
                if (unit.member.name == "localPosition")
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "translation";
                    valueType = GltfTypes.Float3;
                    SpaceConversionHelpers.AddSpaceConversion(unitExporter, out originalValue, out convertedValue);
                    originalValue.MapToInputPort(unit.input);
                }
                if (unit.member.name == "position")
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "translation";
                    valueType = GltfTypes.Float3;
                    SpaceConversionHelpers.AddSpaceConversion(unitExporter, out originalValue, out convertedValue);
                    originalValue.MapToInputPort(unit.input);
                }
                else if (unit.member.name == "localRotation")
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "rotation";
                    valueType = GltfTypes.Float4;
                    SpaceConversionHelpers.AddRotationSpaceConversion(unitExporter, out originalValue, out convertedValue);
                    originalValue.MapToInputPort(unit.input);
                }
                else if (unit.member.name == "rotation")
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "rotation";
                    valueType = GltfTypes.Float4;
                    SpaceConversionHelpers.AddRotationSpaceConversion(unitExporter, out originalValue, out convertedValue);
                    originalValue.MapToInputPort(unit.input);
                }
                else if (unit.member.name == "localScale")
                {
                    pointerTemplate = PointersHelper.IdPointerTemplNodeByRef + "scale";
                    valueType = GltfTypes.Float3;
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
                    pointerTemplate = materialTemplate + gltfProperty;
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