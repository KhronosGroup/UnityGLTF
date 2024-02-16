using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.AI;

namespace UnityGLTF.Plugins
{
    public class MaterialPointerPropertyMap
    {
        public string[] PropertyNames = new string[0];

        internal int[] PropertyIds = new int[0];
        internal int[] PropertyTextureIds = new int[0];

        public string GltfPropertyName = null;
        public string GltfSecondaryPropertyName = null;

        public bool IsTexture = false;
        public bool IsTextureTransform = false;
        public bool PrimaryAndSecondaryGetsCombined = false;
        
        public bool ExportKeepColorAlpha = true;
        public bool ExportConvertToLinearColor = false;
        public bool ExportFlipValueRange = false;
        public float ExportValueMultiplier = 1f;
        public string ExtensionName = null;

        public delegate float[] CombinePrimaryAndSecondary(float[] primary, float[] secondary);
        
        public CombinePrimaryAndSecondary CombinePrimaryAndSecondaryFunction = null;
        
        internal void CreatePropertyIds()
        {
            PropertyIds = new int[PropertyNames.Length];
            for (int i = 0; i < PropertyNames.Length; i++)
                PropertyIds[i] = Shader.PropertyToID(PropertyNames[i]);

            if (IsTextureTransform)
            {
                PropertyTextureIds = new int[PropertyNames.Length];
                for (int i = 0; i < PropertyNames.Length; i++)
                {
                    var pWithoutST = PropertyNames[i].Remove(PropertyNames[i].Length - 3, 3);

                    PropertyTextureIds[i] = Shader.PropertyToID(pWithoutST);
                }
            }
        }
    }

    public class MaterialPropertiesRemapper
    {
        private Dictionary<string, MaterialPointerPropertyMap> maps =
            new Dictionary<string, MaterialPointerPropertyMap>();

        public void AddMap(MaterialPointerPropertyMap map)
        {
            map.CreatePropertyIds();
            if (maps.ContainsKey(map.GltfPropertyName))
                return;
            
            maps.Add(map.GltfPropertyName, map);
        }
        
        public bool GetMapFromUnityMaterial(Material mat, string unityPropertyName, out MaterialPointerPropertyMap map)
        {
            map = null;
            foreach (var kvp in maps)
            {
                if (kvp.Value.PropertyNames.Contains(unityPropertyName))
                {
                    if (kvp.Value.IsTexture)
                    {
                        bool valid = false;
                        for (int i = 0; i < kvp.Value.PropertyIds.Length; i++)
                            valid &= (mat.HasProperty(kvp.Value.PropertyIds[i]) && mat.GetTexture(kvp.Value.PropertyIds[i]));
                        if (!valid)
                            return false;
                    }

                    map = kvp.Value;

                    return true;
                }
            }

            return false;
        }

        public bool GetUnityPropertyName(Material mat, string gltfPropertyName, out string propertyName,
            out MaterialPointerPropertyMap map, out bool isSecondary)
        {
            foreach (var kvp in maps)
            {
                var currentMap = kvp.Value;
                if (currentMap.GltfPropertyName != gltfPropertyName && currentMap.GltfSecondaryPropertyName != gltfPropertyName)
                    continue;

                for (int i = 0; i < currentMap.PropertyNames.Length; i++)
                {
                    if (currentMap.IsTextureTransform)
                    {
                        for (int j = 0; j < currentMap.PropertyNames.Length; j++)
                        {
                            if (mat.HasProperty(currentMap.PropertyTextureIds[j]))
                            {
                                map = currentMap;
                                propertyName = currentMap.PropertyNames[j];
                                isSecondary = currentMap.GltfSecondaryPropertyName == gltfPropertyName;
                                return true;
                            }
                        }
                    }
                    else if (mat.HasProperty(currentMap.PropertyIds[i]))
                    {
                        map = currentMap;
                        propertyName = currentMap.PropertyNames[i];
                        isSecondary = currentMap.GltfSecondaryPropertyName == gltfPropertyName;
                        return true;
                    }
                }
            }

            map = null;
            propertyName = "";
            isSecondary = false;
            return false;
        }
    }


    public class DefaultMaterialPropertiesRemapper : MaterialPropertiesRemapper
    {
        public DefaultMaterialPropertiesRemapper()
        {
            var baseColor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_Color", "_BaseColor", "_BaseColorFactor", "baseColorFactor" },
                ExportConvertToLinearColor = true,
                GltfPropertyName = "pbrMetallicRoughness/baseColorFactor"
            };
            AddMap(baseColor);

            var smoothness = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_Smoothness", "_Glossiness" },
                ExportFlipValueRange = true,
                GltfPropertyName = "pbrMetallicRoughness/roughnessFactor"
            };
            AddMap(smoothness);

            var roughness = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_Roughness", "_RoughnessFactor", "roughnessFactor" },
                GltfPropertyName = "pbrMetallicRoughness/roughnessFactor"
            };
            AddMap(roughness);

            var metallic = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_Metallic", "_MetallicFactor", "metallicFactor" },
                GltfPropertyName = "pbrMetallicRoughness/metallicFactor"
            };
            AddMap(metallic);

            var baseColorTexture = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_MainTex_ST", "_BaseMap_ST", "_BaseColorTexture_ST", "baseColorTexture_ST" },
                IsTexture = true,
                IsTextureTransform = true,
                GltfPropertyName =
                    $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                GltfSecondaryPropertyName =
                    $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                ExtensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
            };
            AddMap(baseColorTexture);

            var emissiveFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_EmissionColor", "_EmissiveFactor", "emissiveFactor" },
                GltfPropertyName = "emissiveFactor",
                GltfSecondaryPropertyName =
                    $"extensions/{KHR_materials_emissive_strength_Factory.EXTENSION_NAME}/{nameof(KHR_materials_emissive_strength.emissiveStrength)}",
                ExtensionName = KHR_materials_emissive_strength_Factory.EXTENSION_NAME,
                PrimaryAndSecondaryGetsCombined = true,
                ExportKeepColorAlpha = false,
                ExportConvertToLinearColor = true,
                CombinePrimaryAndSecondaryFunction = (primary, secondary) =>
                {
                    var result = new float[primary.Length];
                    for (int i = 0; i < 3; i++)
                        result[i] = primary[i] * secondary[0];
                    if (result.Length == 4)
                        result[3] = primary[3];
                    return result;
                }
            };
            AddMap(emissiveFactor);

            var emissiveTexture = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_EmissionMap_ST", "_EmissiveTexture_ST", "emissiveTexture_ST" },
                IsTexture = true,
                IsTextureTransform = true,
                GltfPropertyName =
                    $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                GltfSecondaryPropertyName =
                    $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                ExtensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
            };
            AddMap(emissiveTexture);

            /*var roughnessTex = new MaterialPointerPropertyMap
            {
                propertyNames = new[] { "_BumpMap_ST", "_NormalTexture_ST", "normalTexture_ST" },
                isTexture = true,
                isTextureTransform = true,
                gltfPropertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                gltfSecondaryPropertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                extensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
            };
            
            maps.Add(roughnessTex);		*/

            var alphaCutoff = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_AlphaCutoff", "alphaCutoff", "_Cutoff" },
                GltfPropertyName = "alphaCutoff"
            };
            AddMap(alphaCutoff);

            var normalScale = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_BumpScale", "_NormalScale", "normalScale", "normalTextureScale" },
                GltfPropertyName = "normalTexture/scale"
            };
            AddMap(normalScale);

            var normalTexture = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_BumpMap_ST", "_NormalTexture_ST", "normalTexture_ST" },
                IsTexture = true,
                IsTextureTransform = true,
                GltfPropertyName =
                    $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                GltfSecondaryPropertyName =
                    $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                ExtensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
            };
            AddMap(normalTexture);

            var occlusionStrength = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_OcclusionStrength", "occlusionStrength", "occlusionTextureStrength" },
                GltfPropertyName = "occlusionTexture/strength"
            };
            AddMap(occlusionStrength);

            var occlusionTexture = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_OcclusionMap_ST", "_OcclusionTexture_ST", "occlusionTexture_ST" },
                IsTexture = true,
                IsTextureTransform = true,
                GltfPropertyName =
                    $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                GltfSecondaryPropertyName =
                    $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                ExtensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
            };
            AddMap(occlusionTexture);

            // KHR_materials_transmission
            var transmissionFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_TransmissionFactor", "transmissionFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_transmission_Factory.EXTENSION_NAME}/{nameof(KHR_materials_transmission.transmissionFactor)}",
                ExtensionName = KHR_materials_transmission_Factory.EXTENSION_NAME
            };
            AddMap(transmissionFactor);

            // KHR_materials_volume
            var thicknessFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_ThicknessFactor", "thicknessFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.thicknessFactor)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME
            };
            AddMap(thicknessFactor);

            var attenuationDistance = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_AttenuationDistance", "attenuationDistance" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationDistance)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME
            };
            AddMap(attenuationDistance);

            var attenuationColor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_AttenuationColor", "attenuationColor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationColor)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME,
                ExportKeepColorAlpha = false
            };
            AddMap(attenuationColor);

            // KHR_materials_ior
            var ior = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_IOR", "ior" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_ior_Factory.EXTENSION_NAME}/{nameof(KHR_materials_ior.ior)}",
                ExtensionName = KHR_materials_ior_Factory.EXTENSION_NAME
            };
            AddMap(ior);

            // KHR_materials_iridescence
            var iridescenceFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_IridescenceFactor", "iridescenceFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceFactor)}",
                ExtensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
            };
            AddMap(iridescenceFactor);

            // KHR_materials_specular
            var specularFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_SpecularFactor", "specularFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularFactor)}",
                ExtensionName = KHR_materials_specular_Factory.EXTENSION_NAME
            };
            AddMap(specularFactor);

            var specularColorFactor = new MaterialPointerPropertyMap
            {
                PropertyNames = new[] { "_SpecularColorFactor", "specularColorFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularColorFactor)}",
                ExtensionName = KHR_materials_specular_Factory.EXTENSION_NAME,
                ExportKeepColorAlpha = false
            };
            AddMap(specularColorFactor);


            // TODO KHR_materials_clearcoat
            // case "_ClearcoatFactor":
            // case "clearcoatFactor":
            // 	propertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatFactor)}";
            //	extensionName = KHR_materials_clearcoat_Factory.EXTENSION_NAME;
            // 	break;
            // case "_ClearcoatRoughnessFactor":
            // case "clearcoatRoughnessFactor":
            // 	propertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatRoughnessFactor)}";
            //	extensionName = KHR_materials_clearcoat_Factory.EXTENSION_NAME;
            // 	break;

            // TODO KHR_materials_sheen
            // case "_SheenColorFactor":
            // case "sheenColorFactor":
            // 	propertyName = $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenColorFactor)}";
            //	extensionName = KHR_materials_sheen_Factory.EXTENSION_NAME;
            //	keepColorAlpha = false;
            // 	break;
            // case "_SheenRoughnessFactor":
            // case "sheenRoughnessFactor":
            // 	propertyName = $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenRoughnessFactor)}";
            //	extensionName = KHR_materials_sheen_Factory.EXTENSION_NAME;
            // 	break;
        }
    }
}