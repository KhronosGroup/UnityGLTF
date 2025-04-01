using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity;
using UnityGLTF.Interactivity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Interactivity.Schema;

namespace Editor.UnitExporters.GltfInteractivityUnits
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
            // TODO: worlds space conversion
            
            var unit = unitExporter.unit as InterpolateMember;

            string pointerTemplate = null;
            string pointerId = null;
            GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedValue = null;
            
            var valueType = GltfTypes.Float;
            if (unit.member.targetType == typeof(Transform))
            {
                pointerId = PointersHelper.IdPointerNodeIndex;
                // TODO: transform space conversion for targetValue!!!
                if (unit.member.name == "localPosition")
                {
                    pointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation";
                    valueType = GltfTypes.Float3;
                    SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, unit.input, out convertedValue);
                }
                if (unit.member.name == "position")
                {
                    pointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/translation";
                    valueType = GltfTypes.Float3;
                    SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, unit.input, out convertedValue);
                }
                else if (unit.member.name == "localRotation")
                {
                    pointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation";
                    valueType = GltfTypes.Float4;
                    SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, unit.input, out convertedValue);
                }
                else if (unit.member.name == "rotation")
                {
                    pointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation";
                    valueType = GltfTypes.Float4;
                    SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, unit.input, out convertedValue);
                }
                else if (unit.member.name == "localScale")
                {
                    pointerTemplate = "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/scale";
                    valueType = GltfTypes.Float3;
                    SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, unit.input, out convertedValue);
                }
            }
            
            if (string.IsNullOrEmpty(pointerTemplate))
            { 
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target type for InterpolateMember. Maybe it's not supported.");
                return false;
            }
            
            var node = unitExporter.CreateNode(new Pointer_InterpolateNode());

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
            
            PointersHelper.SetupPointerTemplateAndTargetInput(node, pointerId, unit.target, pointerTemplate, valueType);
            return true;
        }
    }
}