using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Material_GetFloatUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.GetFloat), new Material_GetFloatUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            if (unit.target == null)
                return false;
            
            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            string template = "";
            bool oneMinus = false;
            
            if (unitExporter.IsInputLiteralOrDefaultValue(unit.inputParameters[0], out var floatPropertyName))
            {
                var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, (string)floatPropertyName, out var map);
                if (gltfProperty == null)
                {
                    UnitExportLogging.AddErrorLog(unit, "float property name is not supported.");
                    return false;
                }
                template = materialTemplate + gltfProperty;
                oneMinus = map.ExportFlipValueRange;
            }
            else
            {
                UnitExportLogging.AddErrorLog(unit, "float property name is not a literal or default value, which is not supported.");
                return false;
            }

            var node = unitExporter.CreateNode<Pointer_GetNode>();
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            
            if (oneMinus)
            {
                var oneMinusNode = unitExporter.CreateNode<Math_SubNode>();
                oneMinusNode.ValueIn("a").SetValue(1f).SetType(TypeRestriction.LimitToFloat);
                oneMinusNode.ValueIn("b").ConnectToSource(node.FirstValueOut()).SetType(TypeRestriction.LimitToFloat);
                
                oneMinusNode.FirstValueOut().MapToPort(unit.result).ExpectedType(ExpectedType.Float);
            }
            else
                node.FirstValueOut().MapToPort(unit.result).ExpectedType(ExpectedType.Float);

 
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                unit.target, template, GltfTypes.Float);
            return true;
        }
    }
}