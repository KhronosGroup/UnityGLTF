using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Material_SetFloatUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.SetFloat), new Material_SetFloatUnitExport());
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

            var node = unitExporter.CreateNode<Pointer_SetNode>();
            node.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.exit);

            if (oneMinus)
            {
                var oneMinusNode = unitExporter.CreateNode<Math_SubNode>();
                oneMinusNode.ValueIn("a").SetValue(1f).SetType(TypeRestriction.LimitToFloat);
                oneMinusNode.ValueIn("b").MapToInputPort(unit.inputParameters[1]).SetType(TypeRestriction.LimitToFloat);
                node.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(oneMinusNode.FirstValueOut());
            }
            else
                node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.inputParameters[1]).SetType(TypeRestriction.LimitToFloat);
 
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                unit.target, template, GltfTypes.Float);
            
            return true;
        }
    }
}