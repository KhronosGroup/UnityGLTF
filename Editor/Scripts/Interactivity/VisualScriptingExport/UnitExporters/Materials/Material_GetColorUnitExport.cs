using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Material_GetColorUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.color), new Material_GetColorUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.GetColor), new Material_GetColorUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MemberUnit;
            
            if (unit.target == null)
                return false;
            
            
            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            var template = materialTemplate+ "pbrMetallicRoughness/baseColorFactor";
            
            if (unit is GetMember getMember)
            {
                var node = unitExporter.CreateNode<Pointer_GetNode>();
                node.FirstValueOut().MapToPort(getMember.value).ExpectedType(ExpectedType.Float4);
                PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                    getMember.target, template, GltfTypes.Float4);
            }
            else if (unit is InvokeMember invokeMember)
            {
                // first parameter is the color property name – so based on that we can determine what pointer to set
                // var colorPropertyName = invokeMember.inputParameters[0];
                bool hasAlpha = true;
     
                if (unitExporter.IsInputLiteralOrDefaultValue(invokeMember.inputParameters[0], out var colorPropertyName))
                {
                    var gltfProperty =  MaterialPointerHelper.GetPointer(unitExporter, (string)colorPropertyName, out var map);
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
                var node = unitExporter.CreateNode<Pointer_GetNode>();
                unitExporter.ByPassFlow(invokeMember.enter, invokeMember.exit);
                if (hasAlpha)
                {
                    node.FirstValueOut().MapToPort(invokeMember.result).ExpectedType(ExpectedType.Float4);
                    PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float4);
                }
                else
                {
                    PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float3);
                    
                    var extract = unitExporter.CreateNode<Math_Extract3Node>();
                    var combine = unitExporter.CreateNode<Math_Combine4Node>();
                    extract.ValueIn("a").ConnectToSource(node.FirstValueOut());
                    
                    combine.ValueIn("a").ConnectToSource(extract.ValueOut("0"));
                    combine.ValueIn("b").ConnectToSource(extract.ValueOut("1"));
                    combine.ValueIn("c").ConnectToSource(extract.ValueOut("2"));
                    combine.ValueIn("d").SetValue(1f);
                    combine.FirstValueOut().MapToPort(invokeMember.result).ExpectedType(ExpectedType.Float4);
                }
            }
            return true;
        }
    }
}