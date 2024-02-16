using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using UnityEngine;

namespace GLTF.Utilities
{
	public class MaterialPointerPropertyMap
		{
			public string[] propertyNames = new string[0];
			
			public int[] propertyIds = new int[0];

			public string[] customPropertyComponents = null;
			
			public string gltfPropertyName = null;
			public string gltfSecondaryPropertyName = null;
			public bool isTexture = false;
			public bool isTextureTransform = false;
			public bool keepColorAlpha = true;
			public string extensionName = null;
			public bool convertToLinearColor = false;
			public bool flipValueRange = false;
			
			public void BuildPropertyIds()
			{
				propertyIds = new int[propertyNames.Length];
				for (int i = 0; i < propertyNames.Length; i++)
					propertyIds[i] = Shader.PropertyToID(propertyNames[i]);
			}
		}

		public class MaterialPointerPropertyRemapper
		{
			public List<MaterialPointerPropertyMap> maps = new List<MaterialPointerPropertyMap>();

			public void AddDefaults()
			{
				var baseColor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_Color", "_BaseColor", "_BaseColorFactor", "baseColorFactor" },
					convertToLinearColor = true,
					gltfPropertyName = "pbrMetallicRoughness/baseColorFactor"
				};
				maps.Add(baseColor);
				
				var smoothness = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_Smoothness", "_Glossiness" },
					flipValueRange = true,
					gltfPropertyName = "pbrMetallicRoughness/roughnessFactor"
				};
				maps.Add(smoothness);
				
				var roughness = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_Roughness", "_RoughnessFactor", "roughnessFactor" },
					gltfPropertyName = "pbrMetallicRoughness/roughnessFactor"
				};
				
				maps.Add(roughness);
				
				var metallic = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_Metallic", "_MetallicFactor", "metallicFactor" },
					gltfPropertyName = "pbrMetallicRoughness/metallicFactor"
				};
				maps.Add(metallic);
				
				var baseColorTexture = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_MainTex_ST", "_BaseMap_ST", "_BaseColorTexture_ST", "baseColorTexture_ST" },
					isTexture = true,
					isTextureTransform = true,
					gltfPropertyName = $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
					gltfSecondaryPropertyName = $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
					extensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
				};
				maps.Add(baseColorTexture);
				
				var emissiveFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_EmissionColor", "_EmissiveFactor", "emissiveFactor" },
					gltfPropertyName = "emissiveFactor",
					gltfSecondaryPropertyName = $"extensions/{KHR_materials_emissive_strength_Factory.EXTENSION_NAME}/{nameof(KHR_materials_emissive_strength.emissiveStrength)}",
					extensionName = KHR_materials_emissive_strength_Factory.EXTENSION_NAME,
					keepColorAlpha = false,
					convertToLinearColor = true
				};
				maps.Add(emissiveFactor);
				
				var emissiveTexture = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_EmissionMap_ST", "_EmissiveTexture_ST", "emissiveTexture_ST" },
					isTexture = true,
					isTextureTransform = true,
					gltfPropertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
					gltfSecondaryPropertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
					extensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
				};
				
				maps.Add(emissiveTexture);
				
				var alphaCutoff = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_AlphaCutoff", "alphaCutoff", "_Cutoff" },
					gltfPropertyName = "alphaCutoff"
				};
				maps.Add(alphaCutoff);
				
				var normalScale = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_BumpScale", "_NormalScale", "normalScale", "normalTextureScale" },
					gltfPropertyName = "normalTexture/scale"
				};
				maps.Add(normalScale);
				
				var normalTexture = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_BumpMap_ST", "_NormalTexture_ST", "normalTexture_ST" },
					isTexture = true,
					isTextureTransform = true,
					gltfPropertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
					gltfSecondaryPropertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
					extensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
				};
				
				maps.Add(normalTexture);
				
				var occlusionStrength = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_OcclusionStrength", "occlusionStrength", "occlusionTextureStrength" },
					gltfPropertyName = "occlusionTexture/strength"
				};
				
				maps.Add(occlusionStrength);
				
				var occlusionTexture = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_OcclusionMap_ST", "_OcclusionTexture_ST", "occlusionTexture_ST" },
					isTexture = true,
					isTextureTransform = true,
					gltfPropertyName = $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}",
					gltfSecondaryPropertyName = $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}",
					extensionName = ExtTextureTransformExtensionFactory.EXTENSION_NAME
				};
				
				maps.Add(occlusionTexture);
				
				// KHR_materials_transmission
				var transmissionFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_TransmissionFactor", "transmissionFactor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_transmission_Factory.EXTENSION_NAME}/{nameof(KHR_materials_transmission.transmissionFactor)}",
					extensionName = KHR_materials_transmission_Factory.EXTENSION_NAME
				};
				maps.Add(transmissionFactor);
				
				// KHR_materials_volume
				var thicknessFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_ThicknessFactor", "thicknessFactor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.thicknessFactor)}",
					extensionName = KHR_materials_volume_Factory.EXTENSION_NAME
				};
				maps.Add(thicknessFactor);
				
				var attenuationDistance = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_AttenuationDistance", "attenuationDistance" },
					gltfPropertyName =
						$"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationDistance)}",
					extensionName = KHR_materials_volume_Factory.EXTENSION_NAME
				};
				
				maps.Add(attenuationDistance);
				
				var attenuationColor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_AttenuationColor", "attenuationColor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationColor)}",
					extensionName = KHR_materials_volume_Factory.EXTENSION_NAME,
					keepColorAlpha = false
				};
				
				maps.Add(attenuationColor);
				
				// KHR_materials_ior
				var ior = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_IOR", "ior" },
					gltfPropertyName = $"extensions/{KHR_materials_ior_Factory.EXTENSION_NAME}/{nameof(KHR_materials_ior.ior)}",
					extensionName = KHR_materials_ior_Factory.EXTENSION_NAME
				};
				maps.Add(ior);
				
				// KHR_materials_iridescence
				var iridescenceFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_IridescenceFactor", "iridescenceFactor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceFactor)}",
					extensionName = KHR_materials_iridescence_Factory.EXTENSION_NAME
				};
				
				maps.Add(iridescenceFactor);
				
				// KHR_materials_specular
				var specularFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_SpecularFactor", "specularFactor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularFactor)}",
					extensionName = KHR_materials_specular_Factory.EXTENSION_NAME
				};
				maps.Add(specularFactor);
				
				var specularColorFactor = new MaterialPointerPropertyMap
				{
					propertyNames = new[] { "_SpecularColorFactor", "specularColorFactor" },
					gltfPropertyName =
						$"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularColorFactor)}",
					extensionName = KHR_materials_specular_Factory.EXTENSION_NAME,
					keepColorAlpha = false
				};
				
				maps.Add(specularColorFactor);
			}
			
			public bool GetFromUnityMaterial(Material mat, string unityPropertyName, out string gltfPropertyName, out string extensionName,
				out bool isTextureTransform, out bool keepColorAlpha, out bool convertToLinearColor, out bool flipValueRange)
			{
				gltfPropertyName = "";
				extensionName = "";
				isTextureTransform = false;
				keepColorAlpha = true;
				convertToLinearColor = false;
				flipValueRange = false;

				foreach (var m in maps)
				{
					if (m.propertyNames.Contains(unityPropertyName))
					{
						if (m.isTexture)
						{
							if (m.propertyIds.Length != m.propertyNames.Length)
							{
								m.BuildPropertyIds();
							}
							bool valid = false;
							for (int i = 0; i < m.propertyIds.Length; i++)
								valid &= (mat.HasProperty(m.propertyIds[i]) && mat.GetTexture(m.propertyIds[i]));
							if (!valid)
								return false;
						}
						
						gltfPropertyName = m.gltfPropertyName;
						extensionName = m.extensionName;
						isTextureTransform = m.isTextureTransform;
						keepColorAlpha = m.keepColorAlpha;
						convertToLinearColor = m.convertToLinearColor;
						flipValueRange = m.flipValueRange;
						return true;
					}
				}

				return false;
			}
			
			public bool GetUnityPropertyName(Material mat, string gltfPropertyName, out string propertyName, out MaterialPointerPropertyMap map, out bool isSecondary)
			{
				foreach (var m in maps)
				{
					if (m.gltfPropertyName != gltfPropertyName && m.gltfSecondaryPropertyName != gltfPropertyName)
						continue;
					
					for (int i = 0; i < m.propertyNames.Length; i++)
					{
						if (m.isTextureTransform)
						{
							foreach (var p in m.propertyNames)
							{
								var pWithOutST = p.Remove(p.Length - 3, 3);
								if (mat.HasProperty(pWithOutST))
								{
									map = m;
									propertyName = p;
									isSecondary = m.gltfSecondaryPropertyName == gltfPropertyName;
									return true;
								}
							}
						}
						else
						if (mat.HasProperty(m.propertyNames[i]))
						{
							map = m;
							propertyName = m.propertyNames[i];
							isSecondary = m.gltfSecondaryPropertyName == gltfPropertyName;
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
		
}