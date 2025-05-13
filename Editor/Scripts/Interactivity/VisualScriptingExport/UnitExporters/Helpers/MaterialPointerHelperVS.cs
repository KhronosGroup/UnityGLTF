using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class MaterialPointerHelperVS : MaterialPointerHelper 
    {
        public static void ConvertUvOffsetToGltf(UnitExporter unitExporter, ValueInput targetMaterial, string pointerToTextureTransformScale, out ValueInRef uvOffset, out ValueOutRef convertedUvOffset)
        {
            var getScale = unitExporter.CreateNode<Pointer_GetNode>();
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getScale, PointersHelper.IdPointerMaterialIndex, targetMaterial, pointerToTextureTransformScale, GltfTypes.Float2);
            ConvertUv(unitExporter, out uvOffset, out convertedUvOffset, getScale);       
        }
    }
}