using System;
using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public class MaterialPointerPropertyMap
    {
        public enum PropertyTypeOption
        {
            LinearColor,
            SRGBColor,
            Texture,
            LinearTexture,
            TextureTransform, 
            Float
        }

        public enum CombineResultType
        {
            SameAsPrimary, 
            Override
        }
        
        public PropertyTypeOption PropertyType = PropertyTypeOption.Float;
        
        public string[] PropertyNames
        {
            get => _propertyNames;
            set
            {
                _propertyNames = value;
                CreatePropertyIds();
            }
        }

        public string GltfPropertyName = null;
        public string GltfSecondaryPropertyName = null;

        public bool IsColor => PropertyType == PropertyTypeOption.LinearColor ||
                               PropertyType == PropertyTypeOption.SRGBColor;
        public bool IsTexture => PropertyType == PropertyTypeOption.Texture || 
                                 PropertyType == PropertyTypeOption.LinearTexture;
        
        /// <summary>
        /// When Data is splitted into primary and secondary, the data gets combined on import.
        /// Don't forget to set CombinePrimaryAndSecondaryDataFunction if you set this to true.
        /// </summary>
        public bool CombinePrimaryAndSecondaryOnImport = false;
        
        /// <summary>
        /// Possibility to override the result type of the combined data.
        /// E.g. for combining two Vec2 properties into Vec4 (as for texture transform) 
        /// </summary>
        public CombineResultType CombineComponentResult = CombineResultType.SameAsPrimary;
        public GLTFAccessorAttributeType OverrideCombineResultType = GLTFAccessorAttributeType.VEC4;

        // Export settings
        public bool ExportKeepColorAlpha = true;
        public bool ExportConvertToLinearColor = false;
        public bool ExportFlipValueRange = false;
        public float ExportValueMultiplier = 1f;
        public string ExtensionName = null;

        // The arrays contains the components of the primary and secondary properties. e.g. for a Vector3, the arrays will contain 3 elements
        public delegate float[] CombinePrimaryAndSecondaryData(float[] primary, float[] secondary, int expectedResultLength);

        /// <summary>
        /// Function to combine primary and secondary data on import.
        /// This used by the AnimationPointer system to combine data from two glTF properties into a single Unity property
        /// The arrays contains the components of the primary and secondary properties. e.g. for a Vector3, the arrays will contain 3 elements
        /// </summary>
        public CombinePrimaryAndSecondaryData CombinePrimaryAndSecondaryDataFunction = null;

        private string[] _propertyNames = new string[0];
        internal int[] propertyIds = new int[0];
        internal int[] propertyTextureIds = new int[0];
        
        public MaterialPointerPropertyMap(PropertyTypeOption propertyType)
        {
            PropertyType = propertyType;
            switch (PropertyType)
            {
                case PropertyTypeOption.LinearColor:
                    break;
                case PropertyTypeOption.SRGBColor:
                    ExportConvertToLinearColor = true;
                    break;
                case PropertyTypeOption.Texture:
                    break;
                case PropertyTypeOption.LinearTexture:
                    break;
                case PropertyTypeOption.TextureTransform:
                    SetupAsTextureTransform();
                    break;
                case PropertyTypeOption.Float:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void CreatePropertyIds()
        {
            propertyIds = new int[PropertyNames.Length];
            for (int i = 0; i < PropertyNames.Length; i++)
                propertyIds[i] = Shader.PropertyToID(PropertyNames[i]);

            if (PropertyType == PropertyTypeOption.TextureTransform)
            {
                propertyTextureIds = new int[PropertyNames.Length];
                for (int i = 0; i < PropertyNames.Length; i++)
                {
                    var pWithoutST = PropertyNames[i].Remove(PropertyNames[i].Length - 3, 3);

                    propertyTextureIds[i] = Shader.PropertyToID(pWithoutST);
                }
            }
        }
        
        private static float[] CombineTextureTransform(float[] primary, float[] secondary, int expectedResultLength)
        {
            var result = new float[expectedResultLength];
            
            if (primary.Length >= 2)
            {
                result[0] = primary[0];
                result[1] = primary[1];
            }
            
            if (primary.Length == 0)
            {
                result[0] = 1;
                result[1] = 1;
            }

            if (secondary.Length == 2)
            {
                result[2] = secondary[0];
                result[3] = 1f - secondary[1] - result[1];
            }
            else
            {
                result[2] = 0;
                result[3] = 0;
            }
            return result;
        }
        
        private void SetupAsTextureTransform()
        {
            PropertyType = PropertyTypeOption.TextureTransform;
            CombinePrimaryAndSecondaryDataFunction = CombineTextureTransform;
            CombinePrimaryAndSecondaryOnImport = true;
            CombineComponentResult = MaterialPointerPropertyMap.CombineResultType.Override;
            OverrideCombineResultType = GLTFAccessorAttributeType.VEC4;
        }        
    }

    public class MaterialPropertiesRemapper
    {
        public enum ImportExportUsageOption
        {
            ImportOnly, 
            ExportOnly, 
            ImportAndExport
        }
        
        private Dictionary<string, MaterialPointerPropertyMap> importMaps = new Dictionary<string, MaterialPointerPropertyMap>();
        private Dictionary<string,MaterialPointerPropertyMap> exportMaps = new Dictionary<string,MaterialPointerPropertyMap> ();

        public void AddTextureExtTransforms(string gltfTextureName, string[] unityTextureNames, string extension = null)
        {
            var stNames = new string[unityTextureNames.Length];
            for (int i = 0; i < stNames.Length; i++)
                stNames[i] = $"{unityTextureNames[i]}_ST";
            
            var stMap = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.TextureTransform)
            {
                PropertyNames = stNames,
                GltfPropertyName = $"{gltfTextureName}/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
                GltfSecondaryPropertyName = $"{gltfTextureName}/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
                ExtensionName = extension
            };
            AddMap(stMap);
            
            var rotNames = new string[unityTextureNames.Length];
            for (int i = 0; i < stNames.Length; i++)
                rotNames[i] = $"{unityTextureNames[i]}Rotation";
            
            var rotMap = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = rotNames,
                GltfPropertyName = $"{gltfTextureName}/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.ROTATION}",
                ExtensionName = extension
            };
            AddMap(rotMap);
            
            var texCoordNames = new string[unityTextureNames.Length];
            for (int i = 0; i < stNames.Length; i++)
                texCoordNames[i] = $"{unityTextureNames[i]}TexCoord";
            
            var texCoordMap = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = texCoordNames,
                GltfPropertyName = $"{gltfTextureName}/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.TEXCOORD}",
                ExtensionName = extension
            };
            AddMap(texCoordMap);
        }
        
        public void AddMap(MaterialPointerPropertyMap map, ImportExportUsageOption importExport = ImportExportUsageOption.ImportAndExport)
        {
            if (importExport == ImportExportUsageOption.ImportOnly ||
                importExport == ImportExportUsageOption.ImportAndExport)
            {
                if (importMaps.ContainsKey(map.GltfPropertyName))
                {
                    Debug.LogError("MaterialPropertiesRemapper: Import Map with the same glTF property name already exists: " + map.GltfPropertyName);
                    return;
                }
                importMaps.Add(map.GltfPropertyName, map);
            }
            if (importExport == ImportExportUsageOption.ExportOnly ||
                importExport == ImportExportUsageOption.ImportAndExport)
            {
                for (int i = 0; i < map.PropertyNames.Length; i++)
                {
                    if (exportMaps.ContainsKey(map.PropertyNames[i]))
                    {
                        Debug.LogError("MaterialPropertiesRemapper: Export Map with the same unity property name already exists: " + map.PropertyNames[i]);
                        continue;
                    }
                    
                    exportMaps.Add(map.PropertyNames[i], map);
                }
            }
        }
        
        public bool GetMapByUnityProperty(string unityPropertyName, out MaterialPointerPropertyMap map)
        {
            return exportMaps.TryGetValue(unityPropertyName, out map);
        }
        
        public bool GetMapFromUnityMaterial(Material mat, string unityPropertyName, out MaterialPointerPropertyMap map)
        {
            map = null;
            if (!exportMaps.TryGetValue(unityPropertyName, out map))
                return false;

            if (map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.TextureTransform)
            {
                bool valid = false;
                for (int i = 0; i < map.propertyTextureIds.Length; i++)
                    valid |= (mat.HasProperty(map.propertyTextureIds[i]) && mat.GetTexture(map.propertyTextureIds[i]));
                if (!valid)
                {
                    map = null;
                    return false;
                }
            }
            else
            {
                bool valid = false;
                for (int i = 0; i < map.propertyIds.Length; i++)
                    valid |= (mat.HasProperty(map.propertyIds[i]));
                if (!valid)
                {
                    map = null;
                    return false;
                }
            }

            return true;

        }

        public bool GetUnityPropertyName(Material mat, string gltfPropertyName, out string propertyName,
            out MaterialPointerPropertyMap map, out bool isSecondary)
        {
            foreach (var kvp in importMaps)
            {
                var currentMap = kvp.Value;
                if (currentMap.GltfPropertyName != gltfPropertyName && currentMap.GltfSecondaryPropertyName != gltfPropertyName)
                    continue;

                for (int i = 0; i < currentMap.PropertyNames.Length; i++)
                {
                    if (currentMap.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.TextureTransform)
                    {
                        for (int j = 0; j < currentMap.PropertyNames.Length; j++)
                        {
                            if (mat.HasProperty(currentMap.propertyTextureIds[j]))
                            {
                                map = currentMap;
                                propertyName = currentMap.PropertyNames[j];
                                isSecondary = currentMap.GltfSecondaryPropertyName == gltfPropertyName;
                                return true;
                            }
                        }
                    }
                    else if (mat.HasProperty(currentMap.propertyIds[i]))
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
            var baseColor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
            {
                PropertyNames = new[] { "_Color", "_BaseColor", "_BaseColorFactor", "baseColorFactor" },
                GltfPropertyName = "pbrMetallicRoughness/baseColorFactor",
            };
            AddMap(baseColor);

            var smoothness = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_Smoothness", "_Glossiness" },
                ExportFlipValueRange = true,
                GltfPropertyName = "pbrMetallicRoughness/roughnessFactor",
            };
            AddMap(smoothness, ImportExportUsageOption.ExportOnly);

            var roughness = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_Roughness", "_RoughnessFactor", "roughnessFactor" },
                GltfPropertyName = "pbrMetallicRoughness/roughnessFactor"
            };
            AddMap(roughness);
            
            var dispersion = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "dispersion"},
                GltfPropertyName = "dispersion"
            };
            AddMap(dispersion);            
            
            var metallic = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_Metallic", "_MetallicFactor", "metallicFactor" },
                GltfPropertyName = "pbrMetallicRoughness/metallicFactor"
            };
            AddMap(metallic);
            
            var emissiveFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.SRGBColor)
            {
                // Note: Order changed because in some URP versions Shader Graph declares an extra _EmissionColor property
                // but we want to use the emissiveFactor property.
                PropertyNames = new[] { "emissiveFactor", "_EmissiveFactor", "_EmissionColor" },
                GltfPropertyName = "emissiveFactor",
                GltfSecondaryPropertyName =
                    $"extensions/{KHR_materials_emissive_strength_Factory.EXTENSION_NAME}/{nameof(KHR_materials_emissive_strength.emissiveStrength)}",
                ExtensionName = KHR_materials_emissive_strength_Factory.EXTENSION_NAME,
                CombinePrimaryAndSecondaryOnImport = true,
                ExportKeepColorAlpha = false,
                CombinePrimaryAndSecondaryDataFunction = (primary, secondary, exptedResultLength) =>
                {
                    float strength = (secondary != null && secondary.Length > 0) ? secondary[0] : 1f;
                    var result = new float[exptedResultLength];
                    for (int i = 0; i < 3; i++)
                        result[i] = primary[i] * strength;
                    if (result.Length == 4)
                        result[3] = primary[3];
                    
                    Color color = result.Length == 3 ? new Color(result[0], result[1], result[2]) : new Color(result[0], result[1], result[2], result[3]);
                    color = color.gamma;
                    result[0] = color.r;
                    result[1] = color.g;
                    result[2] = color.b;
                    if (result.Length == 4)
                        result[3] = color.a;
                    return result;
                }
            };
            AddMap(emissiveFactor);
            
            var alphaCutoff = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_AlphaCutoff", "alphaCutoff", "_Cutoff" },
                GltfPropertyName = "alphaCutoff"
            };
            AddMap(alphaCutoff);

            var normalScale = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_BumpScale", "_NormalScale", "normalScale", "normalTextureScale" },
                GltfPropertyName = "normalTexture/scale"
            };
            AddMap(normalScale);
            
            var occlusionStrength = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_OcclusionStrength", "occlusionStrength", "occlusionTextureStrength" },
                GltfPropertyName = "occlusionTexture/strength"
            };
            AddMap(occlusionStrength);

            // KHR_materials_transmission
            var transmissionFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_TransmissionFactor", "transmissionFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_transmission_Factory.EXTENSION_NAME}/{nameof(KHR_materials_transmission.transmissionFactor)}",
                ExtensionName = KHR_materials_transmission_Factory.EXTENSION_NAME
            };
            AddMap(transmissionFactor);

            // KHR_materials_volume
            var thicknessFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_ThicknessFactor", "thicknessFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.thicknessFactor)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME
            };
            AddMap(thicknessFactor);

            var attenuationDistance = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_AttenuationDistance", "attenuationDistance" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationDistance)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME
            };
            AddMap(attenuationDistance);

            var attenuationColor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.LinearColor)
            {
                PropertyNames = new[] { "_AttenuationColor", "attenuationColor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationColor)}",
                ExtensionName = KHR_materials_volume_Factory.EXTENSION_NAME,
                ExportKeepColorAlpha = false,
            };
            AddMap(attenuationColor);

            // KHR_materials_ior
            var ior = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_IOR", "ior" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_ior_Factory.EXTENSION_NAME}/{nameof(KHR_materials_ior.ior)}",
                ExtensionName = KHR_materials_ior_Factory.EXTENSION_NAME
            };
            AddMap(ior);

            // KHR_materials_iridescence
            var iridescenceFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_IridescenceFactor", "iridescenceFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceFactor)}",
                ExtensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
            };
            AddMap(iridescenceFactor);
            var iridescenceIor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "iridescenceIor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceIor)}",
                ExtensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
            };
            AddMap(iridescenceIor);
            var iridescenceThicknessMinimum = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "iridescenceThicknessMinimum" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceThicknessMinimum)}",
                ExtensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
            };
            AddMap(iridescenceThicknessMinimum);
            var iridescenceThicknessMaximum = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "iridescenceThicknessMaximum" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceThicknessMaximum)}",
                ExtensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
            };
            AddMap(iridescenceThicknessMaximum);

            // KHR_materials_specular
            var specularFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_SpecularFactor", "specularFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularFactor)}",
                ExtensionName = KHR_materials_specular_Factory.EXTENSION_NAME
            };
            AddMap(specularFactor);

            var specularColorFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.LinearColor)
            {
                PropertyNames = new[] { "_SpecularColorFactor", "specularColorFactor" },
                GltfPropertyName =
                    $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularColorFactor)}",
                ExtensionName = KHR_materials_specular_Factory.EXTENSION_NAME,
                ExportKeepColorAlpha = false,
            };
            AddMap(specularColorFactor);

            var clearcoatFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_ClearcoatFactor", "clearcoatFactor" },
                GltfPropertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatFactor)}",
                ExtensionName = KHR_materials_clearcoat_Factory.EXTENSION_NAME,
            };
            AddMap(clearcoatFactor);
            
            var clearcoatRoughnessFactor = new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
            {
                PropertyNames = new[] { "_ClearcoatRoughnessFactor", "clearcoatRoughnessFactor" },
                GltfPropertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatRoughnessFactor)}",
                ExtensionName = KHR_materials_clearcoat_Factory.EXTENSION_NAME,
            };
            AddMap(clearcoatRoughnessFactor);

            var sheenRoughnessFactor =
                new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
                {
                    PropertyNames = new[] { "sheenRoughness", "_sheenRoughness", "sheenRoughnessFactor", "_sheenRoughnessFactor" },
                    GltfPropertyName =
                        $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenRoughnessFactor)}",
                    ExtensionName = KHR_materials_sheen_Factory.EXTENSION_NAME,
                };
            AddMap(sheenRoughnessFactor);
            var sheenColorFactor =
                new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
                {
                    PropertyNames = new[] { "sheenColor", "_sheenColor", "sheenColorFactor", "_sheenColorFactor" },
                    GltfPropertyName =
                        $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenColorFactor)}",
                    ExtensionName = KHR_materials_sheen_Factory.EXTENSION_NAME,
                };
            AddMap(sheenColorFactor);
            
            AddTextureExtTransforms("pbrMetallicRoughness/baseColorTexture", new[] { "_MainTex", "_BaseMap", "_BaseColorTexture", "baseColorTexture" });
            AddTextureExtTransforms("emissiveTexture", new[] { "_EmissionMap", "_EmissiveTexture", "emissiveTexture" } );
            AddTextureExtTransforms("normalTexture", new[] { "_BumpMap", "_NormalTexture", "normalTexture" });
            AddTextureExtTransforms("occlusionTexture", new[] { "_OcclusionMap", "_OcclusionTexture", "occlusionTexture" });
            AddTextureExtTransforms("pbrMetallicRoughness/metallicRoughnessTexture", new[] { "metallicRoughnessTexture", "_metallicRoughnessTexture", "_MetallicRoughnessTexture" });

            string clearCoatExt = "extensions/"+nameof(KHR_materials_clearcoat)+"/";
            AddTextureExtTransforms(clearCoatExt+ nameof(KHR_materials_clearcoat.clearcoatTexture), new[] { "_ClearcoatTexture", "clearcoatTexture", "ClearcoatTexture" }, nameof(KHR_materials_clearcoat));
            AddTextureExtTransforms(clearCoatExt+ nameof(KHR_materials_clearcoat.clearcoatRoughnessTexture), new[] { "_ClearcoatRoughnessTexture", "clearcoatRoughnessTexture", "ClearcoatRoughnessTexture" }, nameof(KHR_materials_clearcoat));
            AddTextureExtTransforms(clearCoatExt+ nameof(KHR_materials_clearcoat.clearcoatNormalTexture), new[] { "_ClearcoatNormalTexture_", "clearcoatNormalTexture", "ClearcoatNormalTexture" }, nameof(KHR_materials_clearcoat));
            
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_volume)+"/"+nameof(KHR_materials_volume.thicknessTexture), new[] {"thicknessTexture", "_thicknessTexture"}, nameof(KHR_materials_volume));
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_transmission)+"/"+nameof(KHR_materials_transmission.transmissionTexture), new[] {"transmissionTexture", "_transmissionTexture"}, nameof(KHR_materials_transmission));

            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_iridescence)+"/"+nameof(KHR_materials_iridescence.iridescenceTexture), new[] { "iridescenceTexture", "_iridescenceTexture"}, nameof(KHR_materials_iridescence));
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_iridescence)+"/"+nameof(KHR_materials_iridescence.iridescenceThicknessTexture), new[] { "iridescenceThicknessTexture", "_iridescenceThicknessTexture"}, nameof(KHR_materials_iridescence));

            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_specular)+"/"+nameof(KHR_materials_specular.specularTexture), new[] { "specularTexture", "_specularTexture"}, nameof(KHR_materials_specular));
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_specular)+"/"+nameof(KHR_materials_specular.specularColorTexture), new[] { "specularColorTexture", "_specularColorTexture"}, nameof(KHR_materials_specular));

            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_sheen)+"/"+nameof(KHR_materials_sheen.sheenColorTexture), new[] {"sheenColorTexture", "_sheenColorTexture"}, nameof(KHR_materials_sheen));
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_sheen)+"/"+nameof(KHR_materials_sheen.sheenRoughnessTexture), new[] {"sheenRoughnessTexture", "_sheenRoughnessTexture"}, nameof(KHR_materials_sheen));
            
            AddTextureExtTransforms("extensions/"+nameof(KHR_materials_anisotropy_Factory.EXTENSION_NAME)+"/"+ nameof(KHR_materials_anisotropy.anisotropyTexture), new[] { "anisotropyTexture", "_anisotropyTexture", "anisotropyMap" }, nameof(KHR_materials_clearcoat));
            var anisotropyStrength =
                new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
                {
                    PropertyNames = new[] { "anisotropyStrength", "_anisotropyStrength", "anisotropyFactor", "_anisotropyFactor" },
                    GltfPropertyName = $"extensions/{KHR_materials_anisotropy_Factory.EXTENSION_NAME}/{nameof(KHR_materials_anisotropy.anisotropyStrength)}",
                    ExtensionName = KHR_materials_anisotropy_Factory.EXTENSION_NAME,
                };
            AddMap(anisotropyStrength);
            var anisotropyRotation =
                new MaterialPointerPropertyMap(MaterialPointerPropertyMap.PropertyTypeOption.Float)
                {
                    PropertyNames = new[] { "anisotropyRotation", "_anisotropyRotation", "anisotropyDirection", "_anisotropyDirection" },
                    GltfPropertyName = $"extensions/{KHR_materials_anisotropy_Factory.EXTENSION_NAME}/{nameof(KHR_materials_anisotropy.anisotropyRotation)}",
                    ExtensionName = KHR_materials_anisotropy_Factory.EXTENSION_NAME,
                };
            AddMap(anisotropyRotation);
            
            
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