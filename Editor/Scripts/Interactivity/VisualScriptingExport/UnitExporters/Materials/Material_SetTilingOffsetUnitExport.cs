using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MaterialSetTilingOffsetNode : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }
        private string property;
        private bool isOffset = false;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.SetTextureOffset), new MaterialSetTilingOffsetNode("offset", true));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Material), nameof(Material.SetTextureScale), new MaterialSetTilingOffsetNode("scale", false));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.mainTextureOffset), new MaterialSetTilingOffsetNode("offset", true));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Material), nameof(Material.mainTextureScale), new MaterialSetTilingOffsetNode("scale", false));
        }
        
        public MaterialSetTilingOffsetNode(string property, bool isOffset)
        {
            this.property = property;
            this.isOffset = isOffset;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            // Regular pointer/set
            ValueInput target = null;

            var materialTemplate = "/materials/{" + PointersHelper.IdPointerMaterialIndex + "}/";
            var template = "pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/" + property;
            var scaleTemplate = "pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/scale";
            
            if (unitExporter.unit is SetMember setMember)
            {
                var node = unitExporter.CreateNode<Pointer_SetNode>();
                target = setMember.target;
                unitExporter.MapInputPortToSocketName(setMember.assign, Pointer_SetNode.IdFlowIn, node);
                unitExporter.MapOutFlowConnectionWhenValid(setMember.assigned, Pointer_SetNode.IdFlowOut, node);
                
                if (isOffset)
                {
                    MaterialPointerHelperVS.ConvertUvOffsetToGltf(unitExporter, target, materialTemplate + scaleTemplate, out var uvOffsetIn, out var uvOffSetOut);
                    uvOffsetIn.MapToInputPort(setMember.input);
                    node.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(uvOffSetOut);
                }
                else
                {
                    unitExporter.MapInputPortToSocketName(setMember.input, Pointer_SetNode.IdValue, node);
                }
                
                PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex, target, materialTemplate + template, GltfTypes.Float2);
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
                        scaleTemplate = map.GltfPropertyName;
                        template = map.GltfSecondaryPropertyName;
                    }
                    else
                    {
                        template = gltfProperty;
                    }
                }
                else
                {
                    UnitExportLogging.AddErrorLog(invokeMember, "texture property name is not a literal or default value, which is not supported.");
                    return false;
                }

                var node = unitExporter.CreateNode<Pointer_SetNode>();
                node.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(invokeMember.enter);
                node.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(invokeMember.exit);

                if (isOffset)
                {
                    MaterialPointerHelperVS.ConvertUvOffsetToGltf(unitExporter, target, materialTemplate + scaleTemplate, out var uvOffsetIn, out var uvOffSetOut);
                    uvOffsetIn.MapToInputPort(invokeMember.inputParameters[1]);
                    node.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(uvOffSetOut);
                }
                else
                {
                    node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(invokeMember.inputParameters[1]);
                }
                PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerMaterialIndex, target, materialTemplate + template, GltfTypes.Float2);
            }
            
            return true;
        }
    }
}