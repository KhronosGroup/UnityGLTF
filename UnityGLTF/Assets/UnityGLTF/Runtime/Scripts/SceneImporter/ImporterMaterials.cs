using System.Collections.Generic;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		protected virtual async Task ConstructMaterial(GLTFMaterial def, int materialIndex)
		{
			IUniformMap mapper;

			const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
			const string unlitExtName = KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME;

			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(specGlossExtName) && def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
			{
				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new SpecGlossMap(CustomShaderName, MaximumLod);
				}
				else
				{
					mapper = new SpecGlossMap(MaximumLod);
				}
			}
			else if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(unlitExtName) && def.Extensions != null && def.Extensions.ContainsKey(unlitExtName))
			{
				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new UnlitMap(CustomShaderName, null, MaximumLod);
				}
				else
				{
#if UNITY_2021_3_OR_NEWER
					mapper = new UnlitGraphMap();
#elif UNITY_2019_1_OR_NEWER
					if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline)
						mapper = new UnlitGraphMap(def.AlphaMode == AlphaMode.BLEND, def.DoubleSided);
					else
						mapper = new UnlitMap(MaximumLod);
#else
					mapper = new UnlitMap(MaximumLod);
#endif
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new MetalRoughMap(CustomShaderName, MaximumLod);
				}
				else
				{
					// do we have URP or Unity 2021.2+? Use the PBR Graph Material!
#if UNITY_2021_3_OR_NEWER
					mapper = new PBRGraphMap();
#elif UNITY_2019_1_OR_NEWER
					if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline)
						mapper = new PBRGraphMap(def.AlphaMode == AlphaMode.BLEND, def.DoubleSided);
					else
						mapper = new MetalRoughMap(MaximumLod);
#else
					mapper = new MetalRoughMap(MaximumLod);
#endif
				}
			}

			mapper.Material.name = def.Name;
			mapper.AlphaMode = def.AlphaMode;
			mapper.AlphaCutoff = def.AlphaCutoff;
			mapper.DoubleSided = def.DoubleSided;
			mapper.Material.SetFloat("_BUILTIN_QueueControl", -1);
			mapper.Material.SetFloat("_QueueControl", -1);

			var mrMapper = mapper as IMetalRoughUniformMap;
			if (def.PbrMetallicRoughness != null && mrMapper != null)
			{
				var pbr = def.PbrMetallicRoughness;

				mrMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

				if (pbr.BaseColorTexture != null)
				{
					TextureId textureId = pbr.BaseColorTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					mrMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					var ext = GetTextureTransform(pbr.BaseColorTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						mrMapper.BaseColorXOffset = offset;
						mrMapper.BaseColorXRotation = ext.Rotation;
						mrMapper.BaseColorXScale = ext.Scale.ToUnityVector2Raw();
						mrMapper.BaseColorXTexCoord = ext.TexCoord;

						mapper.Material.SetKeyword("_TEXTURE_TRANSFORM", true);
					}
				}

				mrMapper.MetallicFactor = pbr.MetallicFactor;
				mrMapper.RoughnessFactor = pbr.RoughnessFactor;

				if (pbr.MetallicRoughnessTexture != null)
				{
					TextureId textureId = pbr.MetallicRoughnessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
					mrMapper.MetallicRoughnessTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.MetallicRoughnessTexCoord = pbr.MetallicRoughnessTexture.TexCoord;

					var ext = GetTextureTransform(pbr.MetallicRoughnessTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						mrMapper.MetallicRoughnessXOffset = offset;
						mrMapper.MetallicRoughnessXRotation = ext.Rotation;
						mrMapper.MetallicRoughnessXScale = ext.Scale.ToUnityVector2Raw();
						mrMapper.MetallicRoughnessXTexCoord = ext.TexCoord;
					}
				}
			}
			// when PbrMetallicRoughness is not defined, default values MUST apply
			else if (def.PbrMetallicRoughness == null && mrMapper != null)
			{
				mrMapper.MetallicFactor = 1;
				mrMapper.RoughnessFactor = 1;
			}

			var sgMapper = mapper as ISpecGlossUniformMap;
			if (sgMapper != null)
			{
				var specGloss = def.Extensions[specGlossExtName] as KHR_materials_pbrSpecularGlossinessExtension;

				sgMapper.DiffuseFactor = specGloss.DiffuseFactor.ToUnityColorRaw();

				if (specGloss.DiffuseTexture != null)
				{
					TextureId textureId = specGloss.DiffuseTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					sgMapper.DiffuseTexture = _assetCache.TextureCache[textureId.Id].Texture;
					sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;

					var ext = GetTextureTransform(specGloss.DiffuseTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						sgMapper.DiffuseXOffset = offset;
						sgMapper.DiffuseXRotation = ext.Rotation;
						sgMapper.DiffuseXScale = ext.Scale.ToUnityVector2Raw();
						sgMapper.DiffuseXTexCoord = ext.TexCoord;
					}
				}

				sgMapper.SpecularFactor = specGloss.SpecularFactor.ToUnityVector3Raw();
				sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

				if (specGloss.SpecularGlossinessTexture != null)
				{
					TextureId textureId = specGloss.SpecularGlossinessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					sgMapper.SpecularGlossinessTexture = _assetCache.TextureCache[textureId.Id].Texture;

					var ext = GetTextureTransform(specGloss.SpecularGlossinessTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						sgMapper.SpecularGlossinessXOffset = offset;
						sgMapper.SpecularGlossinessXRotation = ext.Rotation;
						sgMapper.SpecularGlossinessXScale = ext.Scale.ToUnityVector2Raw();
						sgMapper.SpecularGlossinessXTexCoord = ext.TexCoord;
					}
				}
			}

			var unlitMapper = mapper as IUnlitUniformMap;
			if (unlitMapper != null)
			{
				var pbr = def.PbrMetallicRoughness;
				unlitMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

				if (pbr.BaseColorTexture != null)
				{
					TextureId textureId = pbr.BaseColorTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					unlitMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].Texture;
					unlitMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					var ext = GetTextureTransform(pbr.BaseColorTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						unlitMapper.BaseColorXOffset = offset;
						unlitMapper.BaseColorXRotation = ext.Rotation;
						unlitMapper.BaseColorXScale = ext.Scale.ToUnityVector2Raw();
						unlitMapper.BaseColorXTexCoord = ext.TexCoord;

						unlitMapper.Material.SetKeyword("_TEXTURE_TRANSFORM", true);
					}
				}
			}

			var iorMapper = mapper as IIORMap;
			if (iorMapper != null)
			{
				var ior = GetIOR(def);
				if (ior != null)
				{
					iorMapper.IOR = ior.ior;
				}
			}

			var transmissionMapper = mapper as ITransmissionMap;
			if (transmissionMapper != null)
			{
				var transmission = GetTransmission(def);
				if (transmission != null)
				{
					transmissionMapper.TransmissionFactor = transmission.transmissionFactor;
					var td = await FromTextureInfo(transmission.transmissionTexture);
					transmissionMapper.TransmissionTexture = td.Texture;

					mapper.Material.renderQueue = 3000;
					mapper.Material.SetKeyword("_VOLUME_TRANSMISSION", true);
					mapper.Material.SetFloat("_VOLUME_TRANSMISSION_ON", 1f);
				}
			}

			var volumeMapper = mapper as IVolumeMap;
			if (volumeMapper != null)
			{
				var volume = GetVolume(def);
				if (volume != null)
				{
					volumeMapper.AttenuationColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? volume.attenuationColor.ToUnityColorLinear() : volume.attenuationColor.ToUnityColorRaw();
					volumeMapper.AttenuationDistance = volume.attenuationDistance;
					volumeMapper.ThicknessFactor = volume.thicknessFactor;
					var td = await FromTextureInfo(volume.thicknessTexture);
					volumeMapper.ThicknessTexture = td.Texture;

					mapper.Material.renderQueue = 3000;
					mapper.Material.SetFloat("_VOLUME_ON", 1f);
				}
			}

			var iridescenceMapper = mapper as IIridescenceMap;
			if (iridescenceMapper != null)
			{
				var iridescence = GetIridescence(def);
				if (iridescence != null)
				{
					iridescenceMapper.IridescenceFactor = iridescence.iridescenceFactor;
					iridescenceMapper.IridescenceIor = iridescence.iridescenceIor;
					iridescenceMapper.IridescenceThicknessMinimum = iridescence.iridescenceThicknessMinimum;
					iridescenceMapper.IridescenceThicknessMaximum = iridescence.iridescenceThicknessMaximum;
					var td = await FromTextureInfo(iridescence.iridescenceTexture);
					iridescenceMapper.IridescenceTexture = td.Texture;
					var td2 = await FromTextureInfo(iridescence.iridescenceThicknessTexture);
					iridescenceMapper.IridescenceThicknessTexture = td2.Texture;

					mapper.Material.SetKeyword("_IRIDESCENCE", true);
				}
			}

			var specularMapper = mapper as ISpecularMap;
			if (specularMapper != null)
			{
				var specular = GetSpecular(def);
				if (specular != null)
				{
					specularMapper.SpecularFactor = specular.specularFactor;
					specularMapper.SpecularColorFactor = specular.specularColorFactor.ToUnityColorLinear();
					var td = await FromTextureInfo(specular.specularTexture);
					specularMapper.SpecularTexture = td.Texture;
					var td2 = await FromTextureInfo(specular.specularColorTexture);
					specularMapper.SpecularColorTexture = td2.Texture;

					mapper.Material.SetKeyword("_SPECULAR", true);
				}
			}

			var uniformMapper = mapper as ILitMap;
			if (uniformMapper != null)
			{
				if (def.NormalTexture != null)
				{
					TextureId textureId = def.NormalTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
					uniformMapper.NormalTexture = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.NormalTexCoord = def.NormalTexture.TexCoord;
					uniformMapper.NormalTexScale = def.NormalTexture.Scale;

					var ext = GetTextureTransform(def.NormalTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						uniformMapper.NormalXOffset = offset;
						uniformMapper.NormalXRotation = ext.Rotation;
						uniformMapper.NormalXScale = ext.Scale.ToUnityVector2Raw();
						uniformMapper.NormalXTexCoord = ext.TexCoord;
					}
				}

				if (def.OcclusionTexture != null)
				{
					uniformMapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
					TextureId textureId = def.OcclusionTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true);
					uniformMapper.OcclusionTexture = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.OcclusionTexCoord = def.OcclusionTexture.TexCoord;

					var ext = GetTextureTransform(def.OcclusionTexture);

					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						uniformMapper.OcclusionXOffset = offset;
						uniformMapper.OcclusionXRotation = ext.Rotation;
						uniformMapper.OcclusionXScale = ext.Scale.ToUnityVector2Raw();
						// mapper.OcclusionXTexCoord = ext.TexCoord;

						mapper.Material.SetKeyword("_TEXTURE_TRANSFORM", true);
					}
				}

				if (def.EmissiveTexture != null)
				{
					TextureId textureId = def.EmissiveTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false);
					uniformMapper.EmissiveTexture = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;

					var ext = GetTextureTransform(def.EmissiveTexture);
					if (ext != null)
					{
						var offset = ext.Offset.ToUnityVector2Raw();
						offset.y = 1 - ext.Scale.Y - offset.y;
						uniformMapper.EmissiveXOffset = offset;
						uniformMapper.EmissiveXRotation = ext.Rotation;
						uniformMapper.EmissiveXScale = ext.Scale.ToUnityVector2Raw();
						uniformMapper.EmissiveXTexCoord = ext.TexCoord;
					}
				}

				uniformMapper.EmissiveFactor = QualitySettings.activeColorSpace == ColorSpace.Linear ? def.EmissiveFactor.ToUnityColorLinear() : def.EmissiveFactor.ToUnityColorRaw();

				var emissiveExt = GetEmissiveStrength(def);
				if (emissiveExt != null)
				{
					uniformMapper.EmissiveFactor = uniformMapper.EmissiveFactor * emissiveExt.emissiveStrength;
				}
			}
			var vertColorMapper = mapper.Clone();
			vertColorMapper.VertexColorsEnabled = true;

			// if (mapper is PBRGraphMap pbrGraphMap)
			// {
			// 	MaterialExtensions.ValidateMaterialKeywords(pbrGraphMap.Material);
			// }

			MaterialCacheData materialWrapper = new MaterialCacheData
			{
				UnityMaterial = mapper.Material,
				UnityMaterialWithVertexColor = vertColorMapper.Material,
				GLTFMaterial = def
			};

			if (materialIndex >= 0)
			{
				_assetCache.MaterialCache[materialIndex] = materialWrapper;
			}
			else
			{
				_defaultLoadedMaterial = materialWrapper;
			}
		}

		protected virtual Task ConstructMaterialImageBuffers(GLTFMaterial def)
		{
			var tasks = new List<Task>(8);
			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				if (pbr.BaseColorTexture != null)
				{
					var textureId = pbr.BaseColorTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (pbr.MetallicRoughnessTexture != null)
				{
					var textureId = pbr.MetallicRoughnessTexture.Index;

					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.CommonConstant != null)
			{
				if (def.CommonConstant.LightmapTexture != null)
				{
					var textureId = def.CommonConstant.LightmapTexture.Index;

					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.NormalTexture != null)
			{
				var textureId = def.NormalTexture.Index;
				tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
			}

			if (def.OcclusionTexture != null)
			{
				var textureId = def.OcclusionTexture.Index;

				if (!(def.PbrMetallicRoughness != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
						&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == textureId.Id))
				{
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.EmissiveTexture != null)
			{
				var textureId = def.EmissiveTexture.Index;
				tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
			}

			// pbr_spec_gloss extension
			const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
			if (def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
			{
				var specGlossDef = (KHR_materials_pbrSpecularGlossinessExtension)def.Extensions[specGlossExtName];
				if (specGlossDef.DiffuseTexture != null)
				{
					var textureId = specGlossDef.DiffuseTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}

				if (specGlossDef.SpecularGlossinessTexture != null)
				{
					var textureId = specGlossDef.SpecularGlossinessTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_transmission_Factory.EXTENSION_NAME))
			{
				var transmissionDef = (KHR_materials_transmission)def.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME];
				if (transmissionDef.transmissionTexture != null)
				{
					var textureId = transmissionDef.transmissionTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_volume_Factory.EXTENSION_NAME))
			{
				var transmissionDef = (KHR_materials_volume)def.Extensions[KHR_materials_volume_Factory.EXTENSION_NAME];
				if (transmissionDef.thicknessTexture != null)
				{
					var textureId = transmissionDef.thicknessTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_iridescence_Factory.EXTENSION_NAME))
			{
				var iridescenceDef = (KHR_materials_iridescence) def.Extensions[KHR_materials_iridescence_Factory.EXTENSION_NAME];
				if (iridescenceDef.iridescenceTexture != null)
				{
					var textureId = iridescenceDef.iridescenceTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (iridescenceDef.iridescenceThicknessTexture != null)
				{
					var textureId = iridescenceDef.iridescenceThicknessTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_specular_Factory.EXTENSION_NAME))
			{
				var specularDef = (KHR_materials_specular) def.Extensions[KHR_materials_specular_Factory.EXTENSION_NAME];
				if (specularDef.specularTexture != null)
				{
					var textureId = specularDef.specularTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (specularDef.specularColorTexture != null)
				{
					var textureId = specularDef.specularColorTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			return Task.WhenAll(tasks);
		}

		protected virtual ExtTextureTransformExtension GetTextureTransform(TextureInfo def)
		{
			IExtension extension;
			if (_gltfRoot.ExtensionsUsed != null &&
				_gltfRoot.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME) &&
				def.Extensions != null &&
				def.Extensions.TryGetValue(ExtTextureTransformExtensionFactory.EXTENSION_NAME, out extension))
			{
				return (ExtTextureTransformExtension)extension;
			}
			else return null;
		}

		protected virtual KHR_materials_emissive_strength GetEmissiveStrength(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_emissive_strength_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_emissive_strength) extension;
			}
			else return null;
		}

		protected virtual KHR_materials_transmission GetTransmission(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_transmission_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_transmission_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_transmission) extension;
			}
			return null;
		}

		protected virtual KHR_materials_volume GetVolume(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_volume_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_volume_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_volume) extension;
			}
			return null;
		}

		protected virtual KHR_materials_ior GetIOR(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_ior_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_ior_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_ior) extension;
			}
			return null;
		}

		protected virtual KHR_materials_iridescence GetIridescence(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_iridescence_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_iridescence_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_iridescence) extension;
			}
			return null;
		}

		protected virtual KHR_materials_specular GetSpecular(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_specular_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_specular_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_specular) extension;
			}
			return null;
		}

	}
}
