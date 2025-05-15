using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MaterialColorInterpolateUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(MaterialColorInterpolate); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new MaterialColorInterpolateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MaterialColorInterpolate;
            
            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            var template = materialTemplate+ "pbrMetallicRoughness/baseColorFactor";
            
            var valueType = GltfTypes.Float4;
            
            if (unitExporter.IsInputLiteralOrDefaultValue(unit.valueName, out var colorPropertyName))
            {
                var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, (string)colorPropertyName, out var map);
                if (gltfProperty == null)
                {
                    UnitExportLogging.AddErrorLog(unit, "color property name is not supported.");
                    return false;
                }

                valueType = map.ExportKeepColorAlpha ? GltfTypes.Float4 : GltfTypes.Float3;
                template = materialTemplate + gltfProperty;
            }
            else
            {
                UnitExportLogging.AddErrorLog(unit, "color property name is not a literal or default value, which is not supported.");
                return false;
            } 
            
            var node = unitExporter.CreateNode<Pointer_InterpolateNode>();

            node.FlowIn(Pointer_InterpolateNode.IdFlowIn).MapToControlInput(unit.assign);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOut).MapToControlOutput(unit.assigned);
            
            node.ValueIn(Pointer_InterpolateNode.IdValue).MapToInputPort(unit.targetValue);
            node.ValueIn(Pointer_InterpolateNode.IdDuration).MapToInputPort(unit.duration);
            node.ValueIn(Pointer_InterpolateNode.IdPoint1).MapToInputPort(unit.pointA);
            node.ValueIn(Pointer_InterpolateNode.IdPoint2).MapToInputPort(unit.pointB);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOutDone).MapToControlOutput(unit.done);
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex, unit.target, template, valueType);
            return true;
        }
    }
}