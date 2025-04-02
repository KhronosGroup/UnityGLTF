using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class MaterialPointerHelper 
    {
        public static string GetPointer(UnitExporter unitExporter, string unityMaterialPropertyName, out MaterialPointerPropertyMap map)
        {
            var plugins = unitExporter.exportContext.exporter.Plugins;
            
            var animationPointerExportContext =
                plugins.FirstOrDefault(x => x is AnimationPointerExportContext) as AnimationPointerExportContext;
            
            if (animationPointerExportContext == null)
            {
                Debug.LogError("Please activate the KHR_animation_pointer exporter extension under Project Settings > UnityGLTF > Export. This is required for exporting materials with pointers.");
                map = null;
                return null;
            }
            
            if (animationPointerExportContext.materialPropertiesRemapper.GetMapByUnityProperty(unityMaterialPropertyName, out map))
                return map.GltfPropertyName;
            		
            return null;
        }

        public static void ConvertUvOffsetToGltf(UnitExporter unitExporter, ValueInput targetMaterial, string pointerToTextureTransformScale, out GltfInteractivityUnitExporterNode.ValueInputSocketData uvOffset, out GltfInteractivityUnitExporterNode.ValueOutputSocketData convertedUvOffset)
        {
            var getScale = unitExporter.CreateNode(new Pointer_GetNode());
            PointersHelper.SetupPointerTemplateAndTargetInput(getScale, PointersHelper.IdPointerMaterialIndex, targetMaterial, pointerToTextureTransformScale, GltfTypes.Float2);
                   
            var extractScale = unitExporter.CreateNode(new Math_Extract2Node());
            extractScale.ValueIn(Math_Extract2Node.IdValueIn).ConnectToSource(getScale.FirstValueOut()).SetType(TypeRestriction.LimitToFloat2);

            var extractOffset = unitExporter.CreateNode(new Math_Extract2Node());
            uvOffset = extractOffset.ValueIn(Math_Extract2Node.IdValueIn).SetType(TypeRestriction.LimitToFloat2);
                  
            var sub1 = unitExporter.CreateNode(new Math_SubNode());
            sub1.ValueIn(Math_SubNode.IdValueA).SetValue(1f);
            sub1.ValueIn(Math_SubNode.IdValueB).ConnectToSource(extractOffset.ValueOut(Math_Extract2Node.IdValueOutY));
            
            var sub2 = unitExporter.CreateNode(new Math_SubNode());
            sub2.ValueIn(Math_SubNode.IdValueA).ConnectToSource(sub1.FirstValueOut());
            sub2.ValueIn(Math_SubNode.IdValueB).ConnectToSource(extractScale.ValueOut(Math_Extract2Node.IdValueOutY)).SetType(TypeRestriction.LimitToFloat);
            
            var combine = unitExporter.CreateNode(new Math_Combine2Node());
            combine.ValueIn(Math_Combine2Node.IdValueA).ConnectToSource(extractOffset.ValueOut(Math_Extract2Node.IdValueOutX));
            combine.ValueIn(Math_Combine2Node.IdValueB).ConnectToSource(sub2.FirstValueOut());
            convertedUvOffset = combine.FirstValueOut();
        }

      
    }
}