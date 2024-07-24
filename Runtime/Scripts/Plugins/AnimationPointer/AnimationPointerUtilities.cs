using GLTF;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    internal static class AnimationPointerHelpers
    {
        private static readonly string[] MATERIAL_PROPERTY_COMPONENTS = {"x", "y", "z", "w"};
        private static readonly string[] MATERIAL_COLOR_PROPERTY_COMPONENTS = {"r", "g", "b", "a"};

        internal static bool BuildImportMaterialAnimationPointerData(string pointerPath, MaterialPropertiesRemapper remapper, Material material,
            string gltfProperty, AttributeAccessor accessor, out AnimationPointerData pointerData)
        {
            var valueType = accessor.AccessorId.Value.Type;
            pointerData = new AnimationPointerData();
            pointerData.primaryPath = pointerPath;
            pointerData.primaryData = accessor;
            
            if (!remapper.GetUnityPropertyName(material, gltfProperty, out string unityPropertyName,
                    out MaterialPointerPropertyMap propertyMap, out bool isSecondaryGltfProperty))
                return false;

            pointerData.primaryProperty = propertyMap.GltfPropertyName;
            pointerData.secondaryProperty = propertyMap.GltfSecondaryPropertyName;
                        
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
            
            if (isSecondaryGltfProperty)
            {
                pointerData.primaryPath = "";
                pointerData.primaryData = null;
                pointerData.secondaryPath = pointerPath;
                pointerData.secondaryData = accessor;
            }
            
            switch (valueType)
            {
                case GLTFAccessorAttributeType.SCALAR:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 1, isSecondaryGltfProperty ? 1 : 0 );
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC2:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 2, isSecondaryGltfProperty ? 2 : 0);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC3:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 3, isColor: propertyMap.IsColor);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(frame, primaryComponentCount, propertyMap, pointerDataCopy);
                    break;
                case GLTFAccessorAttributeType.VEC4:
                    pointerData.unityPropertyNames = GetAnimationChannelProperties(unityPropertyName, 4, isColor: propertyMap.IsColor);
                    pointerData.importAccessorContentConversion = (data, frame) => MaterialValueConversion(frame, primaryComponentCount, propertyMap, pointerDataCopy);
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
        
        internal static float[] MaterialValueConversion(int index, int componentCount, MaterialPointerPropertyMap map, AnimationPointerData pointerData)
        {
            int resultComponentCount = componentCount;
            if (map.CombineComponentResult == MaterialPointerPropertyMap.CombineResultType.Override)
            {
                resultComponentCount = map.OverrideCombineResultType.ComponentCount();
            }
            float[] primary = new float[0];
            float[] result = new float[resultComponentCount];
            if (pointerData.primaryData != null)
            {
                var primaryData = pointerData.primaryData.AccessorContent;
                switch (componentCount)
                {
                    case 1:
                        primary = new float[1];
                        primary[0] = primaryData.AsFloats[index];
                        break;
                    case 2:
                        primary = new float[2];
                        var frameData2 = primaryData.AsFloat2s[index];
                        primary[0] = frameData2.x;
                        primary[1] = frameData2.y;
                        break;
                    case 3:
                        primary = new float[3];
                        var frameData3 = primaryData.AsFloat3s[index];
                        if (map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
                        {
                            Color gammaColor = new Color(frameData3.x, frameData3.y, frameData3.z).gamma;
                            primary[0] = gammaColor.r;
                            primary[1] = gammaColor.g;
                            primary[2] = gammaColor.b;
                        }
                        else
                        {
                            primary[0] = frameData3.x;
                            primary[1] = frameData3.y;
                            primary[2] = frameData3.z;
                        }

                        break;
                    case 4:
                        primary = new float[4];
                        var frameData4 = primaryData.AsFloat4s[index];
                        if (map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
                        {
                            Color gammaColor = new Color(frameData4.x, frameData4.y, frameData4.z, frameData4.z).gamma;
                            primary[0] = gammaColor.r;
                            primary[1] = gammaColor.g;
                            primary[2] = gammaColor.b;
                            primary[3] = gammaColor.a;
                        }
                        else
                        {
                            primary[0] = frameData4.x;
                            primary[1] = frameData4.y;
                            primary[2] = frameData4.z;
                            primary[3] = frameData4.w;
                        }

                        break;
                }
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
                            return primary;
                    }
                }

                result = map.CombinePrimaryAndSecondaryDataFunction(primary, secondary, result.Length);
            }
            else
                result = primary;

            return result;
        }
    }    
    
}