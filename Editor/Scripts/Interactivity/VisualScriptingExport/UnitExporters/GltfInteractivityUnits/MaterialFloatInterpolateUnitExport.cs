using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MaterialFloatInterpolateUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(MaterialFloatInterpolate); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new MaterialFloatInterpolateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MaterialFloatInterpolate;
            
            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            var template = materialTemplate;
            
            var valueType = GltfTypes.Float;
            ValueOutRef convertedValue = null;
            
            if (unitExporter.IsInputLiteralOrDefaultValue(unit.valueName, out var floatPropertyName))
            {
                var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, (string)floatPropertyName, out var map);
                if (gltfProperty == null)
                {
                    UnitExportLogging.AddErrorLog(unit, "float property name is not supported.");
                    return false;
                }

                if (map.ExportFlipValueRange)
                {
                    var flipNode = unitExporter.CreateNode<Math_SubNode>();
                    flipNode.ValueIn("a").SetValue(1f);
                    flipNode.ValueIn("b").MapToInputPort(unit.targetValue);
                    convertedValue = flipNode.ValueOut("out").ExpectedType(ExpectedType.Float);
                }
                template = materialTemplate + gltfProperty;
            }
            else
            {
                UnitExportLogging.AddErrorLog(unit, "float property name is not a literal or default value, which is not supported.");
                return false;
            } 
            
            var node = unitExporter.CreateNode<Pointer_InterpolateNode>();

            node.FlowIn(Pointer_InterpolateNode.IdFlowIn).MapToControlInput(unit.assign);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOut).MapToControlOutput(unit.assigned);
            
            if (convertedValue == null)
                node.ValueIn(Pointer_InterpolateNode.IdValue).MapToInputPort(unit.targetValue);
            else
                node.ValueIn(Pointer_InterpolateNode.IdValue).ConnectToSource(convertedValue);
            
            node.ValueIn(Pointer_InterpolateNode.IdDuration).MapToInputPort(unit.duration);
            node.ValueIn(Pointer_InterpolateNode.IdPoint1).MapToInputPort(unit.pointA);
            node.ValueIn(Pointer_InterpolateNode.IdPoint2).MapToInputPort(unit.pointB);
            node.FlowOut(Pointer_InterpolateNode.IdFlowOutDone).MapToControlOutput(unit.done);
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex, unit.target, template, valueType);
            return true;
        }
    }
}