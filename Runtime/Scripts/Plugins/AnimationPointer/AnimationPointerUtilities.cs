using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    internal static class AnimationPointerHelpers
    {
        private static readonly string[] MATERIAL_PROPERTY_COMPONENTS = {"x", "y", "z", "w"};

        internal static bool BuildMaterialAnimationPointerData(MaterialPropertiesRemapper remapper, AnimationPointerData pointerData, Material material, string gltfProperty, GLTFAccessorAttributeType valueType)
        {
            if (!remapper.GetUnityPropertyName(material, gltfProperty, out string unityPropertyName,
                    out MaterialPointerPropertyMap propertyMap, out bool isSecondaryGltfProperty))
                return false;
            
            // e.g. Emission Factor and Emission Color are combined into a single property
            if (isSecondaryGltfProperty && propertyMap.PrimaryAndSecondaryGetsCombined)
                return false;

            if (propertyMap.PrimaryAndSecondaryGetsCombined)
            {
                if (propertyMap.CombinePrimaryAndSecondaryFunction == null)
                    return false;
                pointerData.secondaryPath = propertyMap.GltfSecondaryPropertyName;
            }

            var pointerDataCopy = pointerData;
            switch (valueType)
            {
                case GLTFAccessorAttributeType.SCALAR:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 1, isSecondaryGltfProperty ? 1 : 0 );
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 1, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC2:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 2, isSecondaryGltfProperty ? 2 : 0);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 2, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC3:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 3);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 3, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC4:
                    pointerData.unityProperties = AnimationPointerHelpers.GetAnimationChannelProperties(unityPropertyName, 4);
                    pointerData.conversion = (data, frame) => AnimationPointerHelpers.MaterialValueConversion(data, frame, 4, propertyMap, pointerDataCopy);
                    break;
                default:
                    return false;
            }

            return true;
        }
        
        internal static string[] GetAnimationChannelProperties(string propertyName, int componentCount, int componentOffset = 0)
        {
            var result = new string[ componentCount];
            if (componentCount == 1)
            {
                result[0] = $"material.{propertyName}";
                return result;
            }
                
            for (int iComponent = 0; iComponent < componentCount; iComponent++)
                result[iComponent] = $"material.{propertyName}.{MATERIAL_PROPERTY_COMPONENTS[iComponent+componentOffset]}";
            
            return result;
        }
        
        internal static float[] MaterialValueConversion(NumericArray data, int frame, int componentCount, MaterialPointerPropertyMap map, AnimationPointerData pointerData)
        {
            float[] result = new float[componentCount];
									
            if (map.ExportConvertToLinearColor && (componentCount == 3 || componentCount == 4))
            {
                // TODO: ?
            }
            
            switch (componentCount)
            {
                case 1:
                    result[0] = data.AsFloats[frame];
                    break;
                case 2:
                    var frameData2 = data.AsFloats2[frame];
                    result[0] = frameData2.x;
                    result[1] = frameData2.y;
                    break;
                case 3:
                    var frameData3 = data.AsFloats3[frame];
                    result[0] = frameData3.x;
                    result[1] = frameData3.y;
                    result[2] = frameData3.z;
                    break;
                case 4:
                    var frameData4 = data.AsFloats4[frame];
                    result[0] = frameData4.x;
                    result[1] = frameData4.y;
                    result[2] = frameData4.z;
                    result[3] = frameData4.w;
                    break;
            }

            if (map.PrimaryAndSecondaryGetsCombined && map.CombinePrimaryAndSecondaryFunction != null)
            {
                float[] secondary = new float[0];
                NumericArray secondaryData = pointerData.secondaryData.AccessorContent;
                switch (pointerData.secondaryData.AccessorId.Value.Type)
                {
                    case GLTFAccessorAttributeType.SCALAR:
                        secondary = new float[] { secondaryData.AsFloats[frame] };
                        break;
                    case GLTFAccessorAttributeType.VEC2:
                        var s2 = secondaryData.AsFloats2[frame];
                        secondary = new float[] { s2.x, s2.y };
                        break;
                    case GLTFAccessorAttributeType.VEC3:
                        var s3 = secondaryData.AsFloats3[frame];
                        secondary = new float[] { s3.x, s3.y, s3.z };
                        break;
                    case GLTFAccessorAttributeType.VEC4:
                        var s4 = secondaryData.AsFloats4[frame];
                        secondary = new float[] { s4.x, s4.y, s4.z, s4.w };
                        break;
                    default:
                        return result;
                }
                map.CombinePrimaryAndSecondaryFunction(result, secondary);
            }

            return result;
        }
    }    
    
}