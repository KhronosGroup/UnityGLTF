using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Material_SetColorUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.color), new Material_SetColorUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.SetColor), new Material_SetColorUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MemberUnit;
            
            if (unit.target == null)
                return false;
            
            // Regular pointer/set
            
            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            var template = materialTemplate+ "pbrMetallicRoughness/baseColorFactor";
            
            if (unit is SetMember setMember)
            {
                var node = unitExporter.CreateNode<Pointer_SetNode>();
                unitExporter.MapInputPortToSocketName(setMember.assign, Pointer_SetNode.IdFlowIn, node);
                unitExporter.MapInputPortToSocketName(setMember.input, Pointer_SetNode.IdValue, node);
                unitExporter.MapOutFlowConnectionWhenValid(setMember.assigned, Pointer_SetNode.IdFlowOut, node);

                PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                    setMember.target, template, GltfTypes.Float4);
            }
            else if (unit is InvokeMember invokeMember)
            {
                // first parameter is the color property name – so based on that we can determine what pointer to set
                // var colorPropertyName = invokeMember.inputParameters[0];
                bool hasAlpha = true;
                if (unitExporter.IsInputLiteralOrDefaultValue(invokeMember.inputParameters[0], out var colorPropertyName))
                {
                    var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, (string)colorPropertyName, out var map);
                    if (gltfProperty == null)
                    {
                        UnitExportLogging.AddErrorLog(unit, "color property name is not supported.");
                        return false;
                    }

                    hasAlpha = map.ExportKeepColorAlpha;
                    template = materialTemplate + gltfProperty;
                }
                else
                {
                    UnitExportLogging.AddErrorLog(unit, "color property name is not a literal or default value, which is not supported.");
                    return false;
                } 
                
                var node = unitExporter.CreateNode<Pointer_SetNode>();
                node.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(invokeMember.enter);
                node.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(invokeMember.exit);

                if (hasAlpha)
                {
                    node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(invokeMember.inputParameters[1]).SetType(TypeRestriction.LimitToFloat4);
         
                    PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float4);
                }
                else
                {
                    var extract = unitExporter.CreateNode<Math_Extract4Node>();
                    var combine = unitExporter.CreateNode<Math_Combine3Node>();
                    extract.ValueIn("a").MapToInputPort(invokeMember.inputParameters[1])
                        .SetType(TypeRestriction.LimitToFloat4);
                    
                    combine.ValueIn("a").ConnectToSource(extract.ValueOut("0"));
                    combine.ValueIn("b").ConnectToSource(extract.ValueOut("1"));
                    combine.ValueIn("c").ConnectToSource(extract.ValueOut("2"));
                    
                    node.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(combine.FirstValueOut());
                    PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float3);
                }
            }
            return true;
        }
    }
}