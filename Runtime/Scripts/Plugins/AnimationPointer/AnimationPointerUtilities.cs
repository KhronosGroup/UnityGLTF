using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    internal static class AnimationPointerHelpers
    {
        private static readonly string[] MATERIAL_PROPERTY_COMPONENTS = {"x", "y", "z", "w"};
        private static readonly string[] MATERIAL_COLOR_PROPERTY_COMPONENTS = {"r", "g", "b", "a"};

        internal static bool BuildImportMaterialAnimationPointerData(MaterialPropertiesRemapper remapper, Material material,
            string gltfProperty, GLTFAccessorAttributeType valueType, out AnimationPointerData pointerData)
        {
            pointerData = new AnimationPointerData();
            if (!remapper.GetUnityPropertyName(material, gltfProperty, out string unityPropertyName,
                    out MaterialPointerPropertyMap propertyMap, out bool isSecondaryGltfProperty))
                return false;
            
            // e.g. Emission Factor and Emission Color are combined into a single property
            if (isSecondaryGltfProperty && propertyMap.CombinePrimaryAndSecondaryOnImport)
                return false;

            if (propertyMap.CombinePrimaryAndSecondaryOnImport)
            {
                if (propertyMap.CombinePrimaryAndSecondaryDataFunction == null)
                    return false;
                pointerData.secondaryPath = propertyMap.GltfSecondaryPropertyName;
            }

            var pointerDataCopy = pointerData;

            int primaryComponentCount = valueType.ComponentCount();
            
            if (propertyMap.CombineComponentResult != MaterialPointerPropertyMap.CombineResultType.SameAsPrimary)
            {
                valueType = propertyMap.OverrideCombineResultType;
            }
            
            switch (valueType)
            {
                case GLTFAccessorAttributeType.SCALAR:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 1, isSecondaryGltfProperty ? 1 : 0 );
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(data.primaryData.AccessorContent, frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC2:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 2, isSecondaryGltfProperty ? 2 : 0);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(data.primaryData.AccessorContent, frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC3:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 3, isColor: propertyMap.IsColor);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(data.primaryData.AccessorContent, frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC4:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 4, isColor: propertyMap.IsColor);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(data.primaryData.AccessorContent, frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                default:
                    return false;
            }

            return true;
        }
        
        internal static string[] GetAnimationChannelProperties(string propertyName, int componentCount, int componentOffset = 0, bool isColor = false)
        {
            var result = new string[ componentCount];
            if (componentCount == 1)
            {
                result[0] = $"material.{propertyName}";
                return result;
            }

            for (int iComponent = 0; iComponent < componentCount; iComponent++)
            {
                if (isColor)
                    result[iComponent] = $"material.{propertyName}.{MATERIAL_COLOR_PROPERTY_COMPONENTS[iComponent+componentOffset]}";
                else
                    result[iComponent] = $"material.{propertyName}.{MATERIAL_PROPERTY_COMPONENTS[iComponent+componentOffset]}";
            }
            
            return result;
        }
        
        internal static float[] MaterialValueConversion(NumericArray data, int index, int componentCount, MaterialPointerPropertyMap map, AnimationPointerData pointerData)
        {
            int resultComponentCount = componentCount;
            if (map.CombineComponentResult == MaterialPointerPropertyMap.CombineResultType.Override)
            {
                resultComponentCount = map.OverrideCombineResultType.ComponentCount();
            }
            float[] result = new float[resultComponentCount];

            
            switch (componentCount)
            {
                case 1:
                    result[0] = data.AsFloats[index];
                    break;
                case 2:
                    var frameData2 = data.AsFloat2s[index];
                    result[0] = frameData2.x;
                    result[1] = frameData2.y;
                    break;
                case 3:
                    var frameData3 = data.AsFloat3s[index];
                    if (map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
                    {
                        Color gammaColor = new Color(frameData3.x, frameData3.y, frameData3.z).gamma;
                        result[0] = gammaColor.r;
                        result[1] = gammaColor.g;
                        result[2] = gammaColor.b;
                    }
                    else
                    {
                        result[0] = frameData3.x;
                        result[1] = frameData3.y;
                        result[2] = frameData3.z;
                    }
                    break;
                case 4:
                    var frameData4 = data.AsFloat4s[index];
                    if (map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
                    {
                        Color gammaColor = new Color(frameData4.x, frameData4.y, frameData4.z, frameData4.z).gamma;
                        result[0] = gammaColor.r;
                        result[1] = gammaColor.g;
                        result[2] = gammaColor.b;
                        result[3] = gammaColor.a;
                    }
                    else
                    {
                        result[0] = frameData4.x;
                        result[1] = frameData4.y;
                        result[2] = frameData4.z;
                        result[3] = frameData4.w;
                    }

                    break;
            }
            
            if (map.CombinePrimaryAndSecondaryOnImport && map.CombinePrimaryAndSecondaryDataFunction != null)
            {
                float[] secondary = new float[0];

                if (pointerData.secondaryData != null && pointerData.secondaryData.AccessorContent.AsFloats != null)
                {
                    NumericArray secondaryData = pointerData.secondaryData.AccessorContent;
                    switch (pointerData.secondaryData.AccessorId.Value.Type)
                    {
                        case GLTFAccessorAttributeType.SCALAR:
                            secondary = new float[] { secondaryData.AsFloats[index] };
                            break;
                        case GLTFAccessorAttributeType.VEC2:
                            var s2 = secondaryData.AsFloat2s[index];
                            secondary = new float[] { s2.x, s2.y };
                            break;
                        case GLTFAccessorAttributeType.VEC3:
                            var s3 = secondaryData.AsFloat3s[index];
                            secondary = new float[] { s3.x, s3.y, s3.z };
                            break;
                        case GLTFAccessorAttributeType.VEC4:
                            var s4 = secondaryData.AsFloat4s[index];
                            secondary = new float[] { s4.x, s4.y, s4.z, s4.w };
                            break;
                        default:
                            return result;
                    }
                }

                result = map.CombinePrimaryAndSecondaryDataFunction(result, secondary);
            }

            return result;
        }
    }    
    
}