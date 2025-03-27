using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class Material_GetTilingOffsetUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        private string property;
        private bool isOffset;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.GetTextureOffset), new Material_GetTilingOffsetUnitExport("offset", true));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.GetTextureScale), new Material_GetTilingOffsetUnitExport("scale", false));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.mainTextureOffset), new Material_GetTilingOffsetUnitExport("offset", true));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.mainTextureScale), new Material_GetTilingOffsetUnitExport("scale", false));
        }
        
        public Material_GetTilingOffsetUnitExport(string property, bool isOffset)
        {
            this.property = property;
            this.isOffset = isOffset;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            ValueInput target = null;
            
            var materialTemplate = "/materials/{" + GltfInteractivityNodeHelper.IdPointerMaterialIndex + "}/";
            var template = "pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/"+property;
            var scaleTemplate = "pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/scale";

            if (unitExporter.unit is GetMember getMember)
            {
                var node = unitExporter.CreateNode(new Pointer_GetNode());
                target = getMember.target;
                
                if (isOffset)
                {
                    MaterialPointerHelper.ConvertUvOffsetToGltf(unitExporter, target, scaleTemplate, out var uvOffsetIn, out var uvOffSetOut);
                    uvOffsetIn.ConnectToSource(node.FirstValueOut());
                    uvOffSetOut.MapToPort(getMember.value);
                }
                else
                    unitExporter.MapValueOutportToSocketName(getMember.value, Pointer_GetNode.IdValue, node);

                node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMaterialIndex, target, template, GltfTypes.Float2);
            }
            else if (unitExporter.unit is InvokeMember invokeMember)
            {
                target = invokeMember.target;
                
                if (unitExporter.IsInputLiteralOrDefaultValue(invokeMember.inputParameters[0], out var texturePropertyName))
                {
                    string unityPropertyName = (string)texturePropertyName;
                    if (!unityPropertyName.EndsWith("_ST"))
                        unityPropertyName += "_ST";
                    
                    var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, unityPropertyName, out var map);
                    if (gltfProperty == null)
                    {
                        UnitExportLogging.AddErrorLog(invokeMember, "texture property name is not supported.");
                        return false;
                    }

                    if (isOffset)
                    {
                        template = materialTemplate + map.GltfSecondaryPropertyName;
                        scaleTemplate = materialTemplate + map.GltfPropertyName;
                    }
                    else
                        template = materialTemplate + gltfProperty;
                }
                else
                {
                    UnitExportLogging.AddErrorLog(invokeMember, "texture property name is not a literal or default value, which is not supported.");
                    return false;
                } 
                
                var node = unitExporter.CreateNode(new Pointer_GetNode());
                unitExporter.ByPassFlow(invokeMember.enter, invokeMember.exit);
                node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMaterialIndex, target, template, GltfTypes.Float2);
                
                if (isOffset)
                {
                    MaterialPointerHelper.ConvertUvOffsetToGltf(unitExporter, target, scaleTemplate, out var uvOffsetIn, out var uvOffSetOut);
                    uvOffsetIn.ConnectToSource(node.FirstValueOut());
                    uvOffSetOut.MapToPort(invokeMember.result);
                }
                else
                    unitExporter.MapValueOutportToSocketName(invokeMember.result, Pointer_GetNode.IdValue, node);
                
            }
            return true;
        }
    }
}