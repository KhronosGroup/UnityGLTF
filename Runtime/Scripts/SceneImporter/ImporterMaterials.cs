using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
		internal static List<Texture> _runtimeNormalTextures = new List<Texture>();
		internal List<object> _warnOnce = new List<object>();
		
		protected virtual async Task ConstructMaterial(GLTFMaterial def, int materialIndex)
		{
			IUniformMap mapper;

			const string specGlossExtName = KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME;
			const string unlitExtName = KHR_MaterialsUnlitExtensionFactory.EXTENSION_NAME;

			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(specGlossExtName) && def.Extensions != null && def.Extensions.ContainsKey(specGlossExtName))
			{
				Debug.Log(LogType.Warning, $"KHR_materials_pbrSpecularGlossiness has been deprecated, material {def.Name} may not look correct. Use `gltf-transform metalrough` or other tools to convert to PBR. (File: {_gltfFileName})");

				if (!string.IsNullOrEmpty(CustomShaderName))
				{
					mapper = new SpecGlossMap(CustomShaderName, MaximumLod);
				}
				else
				{
#if UNITY_2021_3_OR_NEWER
					// This isn't fully supported. KHR_materials_pbrSpecularGlossiness is deprecated though, so we're warning here.
					mapper = new PBRGraphMap();
#else
					mapper = new SpecGlossMap(MaximumLod);
#endif
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

			void CalculateYOffsetAndScale(TextureId textureId, ExtTextureTransformExtension ext, out Vector2 scale, out Vector2 offset)
			{
				offset = ext.Offset.ToUnityVector2Raw();
				scale = ext.Scale.ToUnityVector2Raw();

				if (IsTextureFlipped(textureId.Value))
				{
					offset.y =  scale.y + offset.y;
					scale.y *= -1f;
				}
				else
				{
					offset.y = 1 - scale.y - offset.y;
				}
			}

			mapper.Material.name = def.Name;
			mapper.AlphaMode = def.AlphaMode;
			mapper.AlphaCutoff = def.AlphaCutoff;
			mapper.DoubleSided = def.DoubleSided;
			mapper.Material.SetFloat("_BUILTIN_QueueControl", 0);
			mapper.Material.SetFloat("_QueueControl", 0);

#if UNITY_EDITOR
			if (Context.SourceImporter == null)
#endif
			mapper.Material.SetFloat("_NormalMapFormatXYZ", 1);
			
			void SetTransformKeyword()
			{
				MatHelper.SetKeyword(mapper.Material, "_TEXTURE_TRANSFORM", true);
			}
			
#if UNITY_EDITOR
			var tempMapper = mapper;
			
			// Check if the material is valid – broken Shader Graphs import as single-pass magenta shaders...
			var seemsToBeBroken = mapper.Material.shader?.passCount <= 1;
			if (seemsToBeBroken)
			{
				var key = (mapper.Material.shader, Context?.SourceImporter);
				if (!_warnOnce.Contains(key))
				{
					Debug.Log(LogType.Error, 
						(object) $"glTF materials could not be correctly imported because there is an error with shader \"{mapper.Material.shader?.name}\". This is likely caused by Shader Graph keyword limits being too low; increase the Shader Variant Limit in \"Preferences > Shader Graph\", reimport the UnityGLTF package, and then reimport this file.\n\n", Context?.SourceImporter);
					_warnOnce.Add(key);
				}
				// Set mapper to null so we're not trying to set any material properties and causing errors.
				// We're restoring it right before creating the material.
				mapper = null;
			}
#endif
			
			var mrMapper = mapper as IMetalRoughUniformMap;
			if (def.PbrMetallicRoughness != null && mrMapper != null)
			{
				var pbr = def.PbrMetallicRoughness;

				mrMapper.BaseColorFactor = pbr.BaseColorFactor.ToUnityColorRaw();

				if (pbr.BaseColorTexture != null)
				{
					TextureId textureId = pbr.BaseColorTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false, false);
					mrMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					var ext = GetTextureTransform(pbr.BaseColorTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						mrMapper.BaseColorXOffset = offset;
						mrMapper.BaseColorXRotation = ext.Rotation;
						mrMapper.BaseColorXScale = scale;
						if (ext.TexCoord != null) mrMapper.BaseColorXTexCoord = ext.TexCoord.Value;

						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						mrMapper.BaseColorXScale = new Vector2(1f,-1f);
						mrMapper.BaseColorXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}

				mrMapper.MetallicFactor = pbr.MetallicFactor;
				mrMapper.RoughnessFactor = pbr.RoughnessFactor;

				if (pbr.MetallicRoughnessTexture != null)
				{
					TextureId textureId = pbr.MetallicRoughnessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true, false);
					mrMapper.MetallicRoughnessTexture = _assetCache.TextureCache[textureId.Id].Texture;
					mrMapper.MetallicRoughnessTexCoord = pbr.MetallicRoughnessTexture.TexCoord;

					var ext = GetTextureTransform(pbr.MetallicRoughnessTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						mrMapper.MetallicRoughnessXOffset = offset;
						mrMapper.MetallicRoughnessXRotation = ext.Rotation;
						mrMapper.MetallicRoughnessXScale = scale;
						if (ext.TexCoord != null) mrMapper.MetallicRoughnessXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						mrMapper.MetallicRoughnessXScale = new Vector2(1f,-1f);
						mrMapper.MetallicRoughnessXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}
			}
			// when PbrMetallicRoughness is not defined, default values MUST apply
			else if (def.PbrMetallicRoughness == null && mrMapper != null)
			{
				mrMapper.MetallicFactor = 1;
				mrMapper.RoughnessFactor = 1;
			}
			
			// get MaterialImportPluginContext and check which options are enabled
			// ReSharper disable InconsistentNaming IdentifierTypo
			var settings = (Context.Plugins.FirstOrDefault(x => x is MaterialExtensionsImportContext) as MaterialExtensionsImportContext)?.settings;
			var KHR_materials_ior = settings && settings.KHR_materials_ior;
			var KHR_materials_transmission = settings && settings.KHR_materials_transmission;
			var KHR_materials_volume = settings && settings.KHR_materials_volume;
			var KHR_materials_iridescence = settings && settings.KHR_materials_iridescence;
			var KHR_materials_specular = settings && settings.KHR_materials_specular;
			var KHR_materials_clearcoat = settings && settings.KHR_materials_clearcoat;
			var KHR_materials_pbrSpecularGlossiness = settings && settings.KHR_materials_pbrSpecularGlossiness;
			var KHR_materials_emissive_strength = settings && settings.KHR_materials_emissive_strength;
			var KHR_materials_sheen = settings && settings.KHR_materials_sheen;
			var KHR_materials_anisotropy = settings && settings.KHR_materials_anisotropy;
			// ReSharper restore InconsistentNaming
			
			var sgMapper = mapper as ISpecGlossUniformMap;
			if (sgMapper != null && KHR_materials_pbrSpecularGlossiness)
			{
				var specGloss = def.Extensions[specGlossExtName] as KHR_materials_pbrSpecularGlossinessExtension;

				sgMapper.DiffuseFactor = specGloss.DiffuseFactor.ToUnityColorRaw();

				if (specGloss.DiffuseTexture != null)
				{
					TextureId textureId = specGloss.DiffuseTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false, false);
					sgMapper.DiffuseTexture = _assetCache.TextureCache[textureId.Id].Texture;
					sgMapper.DiffuseTexCoord = specGloss.DiffuseTexture.TexCoord;

					var ext = GetTextureTransform(specGloss.DiffuseTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						sgMapper.DiffuseXOffset = offset;
						sgMapper.DiffuseXRotation = ext.Rotation;
						sgMapper.DiffuseXScale = scale;
						if (ext.TexCoord != null) sgMapper.DiffuseXTexCoord = ext.TexCoord.Value;
						MatHelper.SetKeyword(mapper.Material, "_TEXTURE_TRANSFORM", true);
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						sgMapper.DiffuseXScale = new Vector2(1f,-1f);
						sgMapper.DiffuseXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}

				sgMapper.SpecularFactor = specGloss.SpecularFactor.ToUnityVector3Raw();
				sgMapper.GlossinessFactor = specGloss.GlossinessFactor;

				if (specGloss.SpecularGlossinessTexture != null)
				{
					TextureId textureId = specGloss.SpecularGlossinessTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false, false);
					sgMapper.SpecularGlossinessTexture = _assetCache.TextureCache[textureId.Id].Texture;
					sgMapper.SpecularGlossinessTexCoord = specGloss.SpecularGlossinessTexture.TexCoord;

					var ext = GetTextureTransform(specGloss.SpecularGlossinessTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						sgMapper.SpecularGlossinessXOffset = offset;
						sgMapper.SpecularGlossinessXRotation = ext.Rotation;
						sgMapper.SpecularGlossinessXScale = scale;
						if (ext.TexCoord != null) sgMapper.SpecularGlossinessXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						sgMapper.SpecularGlossinessXScale = new Vector2(1f,-1f);
						sgMapper.SpecularGlossinessXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
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
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false, false);
					unlitMapper.BaseColorTexture = _assetCache.TextureCache[textureId.Id].Texture;
					unlitMapper.BaseColorTexCoord = pbr.BaseColorTexture.TexCoord;

					var ext = GetTextureTransform(pbr.BaseColorTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						unlitMapper.BaseColorXOffset = offset;
						unlitMapper.BaseColorXRotation = ext.Rotation;
						unlitMapper.BaseColorXScale = scale;
						if (ext.TexCoord != null) unlitMapper.BaseColorXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						unlitMapper.BaseColorXScale = new Vector2(1f,-1f);
						unlitMapper.BaseColorXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}
			}

			var iorMapper = mapper as IIORMap;
			if (iorMapper != null && KHR_materials_ior)
			{
				var ior = GetIOR(def);
				if (ior != null)
				{
					iorMapper.IOR = ior.ior;
				}
			}

			var sheenMapper = mapper as ISheenMap;
			if (sheenMapper != null && KHR_materials_sheen)
			{
				var sheen = GetSheen(def);
				if (sheen != null)
				{
					sheenMapper.SheenColorFactor = sheen.sheenColorFactor.ToUnityColorRaw();
					sheenMapper.SheenRoughnessFactor = sheen.sheenRoughnessFactor;
					MatHelper.SetKeyword(mapper.Material, "_SHEEN", true);
					
					if (sheen.sheenColorTexture != null)
					{
						var td = await FromTextureInfo(sheen.sheenColorTexture, false);
						sheenMapper.SheenColorTexture = td.Texture;
						sheenMapper.SheenColorTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(sheen.sheenColorTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(sheen.sheenColorTexture.Index, ext, out var scale, out var offset);
							sheenMapper.SheenColorTextureOffset = offset;
							sheenMapper.SheenColorTextureScale = scale;
							sheenMapper.SheenColorTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) sheenMapper.SheenColorTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(sheen.sheenColorTexture.Index.Value))
						{
							sheenMapper.SheenColorTextureScale = new Vector2(1f,-1f);
							sheenMapper.SheenColorTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}
					
					if (sheen.sheenRoughnessTexture != null)
					{
						var td = await FromTextureInfo(sheen.sheenRoughnessTexture, false);
						sheenMapper.SheenRoughnessTexture = td.Texture;
						sheenMapper.SheenColorTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(sheen.sheenRoughnessTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(sheen.sheenRoughnessTexture.Index, ext, out var scale, out var offset);
							sheenMapper.SheenRoughnessTextureOffset = offset;
							sheenMapper.SheenRoughnessTextureScale = scale;
							sheenMapper.SheenRoughnessTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) sheenMapper.SheenRoughnessTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(sheen.sheenRoughnessTexture.Index.Value))
						{
							sheenMapper.SheenRoughnessTextureScale = new Vector2(1f,-1f);
							sheenMapper.SheenRoughnessTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}
				}
			}
			
			var anisotropyMapper = mapper as IAnisotropyMap;
			if (anisotropyMapper != null && KHR_materials_anisotropy)
			{
				var anisotropy = GetAnisotropy(def);
				if (anisotropy != null)
				{
					anisotropyMapper.anisotropyRotation = anisotropy.anisotropyRotation;
					anisotropyMapper.anisotropyStrength = anisotropy.anisotropyStrength;
					
					MatHelper.SetKeyword(mapper.Material, "_ANISOTROPY", true );
					
					if (anisotropy.anisotropyTexture != null)
					{
						var td = await FromTextureInfo(anisotropy.anisotropyTexture, false);
						anisotropyMapper.anisotropyTexture = td.Texture;
						anisotropyMapper.anisotropyTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(anisotropy.anisotropyTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(anisotropy.anisotropyTexture.Index, ext, out var scale, out var offset);
							anisotropyMapper.anisotropyTextureOffset = offset;
							anisotropyMapper.anisotropyTextureScale = scale;
							anisotropyMapper.anisotropyTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) anisotropyMapper.anisotropyTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(anisotropy.anisotropyTexture.Index.Value))
						{
							anisotropyMapper.anisotropyTextureScale = new Vector2(1f,-1f);
							anisotropyMapper.anisotropyTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}
				}
			}
			
			var transmissionMapper = mapper as ITransmissionMap;
			if (transmissionMapper != null && KHR_materials_transmission)
			{
				var transmission = GetTransmission(def);
				if (transmission != null)
				{
					transmissionMapper.TransmissionFactor = transmission.transmissionFactor;

					if (transmission.transmissionTexture != null)
					{
						var td = await FromTextureInfo(transmission.transmissionTexture, false);
						transmissionMapper.TransmissionTexture = td.Texture;
						transmissionMapper.TransmissionTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(transmission.transmissionTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(transmission.transmissionTexture.Index, ext, out var scale, out var offset);
							transmissionMapper.TransmissionTextureOffset = offset;
							transmissionMapper.TransmissionTextureScale = scale;
							transmissionMapper.TransmissionTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) transmissionMapper.TransmissionTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(transmission.transmissionTexture.Index.Value))
						{
							transmissionMapper.TransmissionTextureScale = new Vector2(1f,-1f);
							transmissionMapper.TransmissionTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					mapper.Material.renderQueue = 3000;
#if UNITY_VISIONOS
					mapper.AlphaMode = AlphaMode.BLEND;
#endif
					bool hasDispersion = false;
					if (transmissionMapper is IDispersionMap dispersionMapper)
					{
						var dispersion = GetDispersion(def);
						if (dispersion != null)
						{
							hasDispersion = true;
							dispersionMapper.Dispersion = dispersion.dispersion;
						}
					}

					if (hasDispersion)
					{
						mapper.Material.EnableKeyword("_VOLUME_TRANSMISSION_ANDDISPERSION");
						mapper.Material.SetFloat("_VOLUME_TRANSMISSION", 2f);
						
					}
					else
					{
						mapper.Material.EnableKeyword("_VOLUME_TRANSMISSION_ON");
						mapper.Material.SetFloat("_VOLUME_TRANSMISSION", 1f);
					}
				}
			}

			var volumeMapper = mapper as IVolumeMap;
			if (volumeMapper != null && KHR_materials_volume)
			{
				var volume = GetVolume(def);
				if (volume != null)
				{
#if UNITY_VISIONOS
					mapper.AlphaMode = AlphaMode.BLEND;
#endif
					volumeMapper.AttenuationColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? volume.attenuationColor.ToUnityColorLinear() : volume.attenuationColor.ToUnityColorRaw();
					volumeMapper.AttenuationDistance = volume.attenuationDistance;
					volumeMapper.ThicknessFactor = volume.thicknessFactor;
					if (volume.thicknessTexture != null)
					{
						var td = await FromTextureInfo(volume.thicknessTexture, false);
						volumeMapper.ThicknessTexture = td.Texture;
						volumeMapper.ThicknessTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(volume.thicknessTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(volume.thicknessTexture.Index, ext, out var scale, out var offset);
							volumeMapper.ThicknessTextureOffset = offset;
							volumeMapper.ThicknessTextureScale = scale;
							volumeMapper.ThicknessTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) volumeMapper.ThicknessTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(volume.thicknessTexture.Index.Value))
						{
							volumeMapper.ThicknessTextureScale = new Vector2(1f,-1f);
							volumeMapper.ThicknessTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					mapper.Material.renderQueue = 3000;
					mapper.Material.SetFloat("_VOLUME_ON", 1f);
				}
			}

			var iridescenceMapper = mapper as IIridescenceMap;
			if (iridescenceMapper != null && KHR_materials_iridescence)
			{
				var iridescence = GetIridescence(def);
				if (iridescence != null)
				{
					iridescenceMapper.IridescenceFactor = iridescence.iridescenceFactor;
					iridescenceMapper.IridescenceIor = iridescence.iridescenceIor;
					iridescenceMapper.IridescenceThicknessMinimum = iridescence.iridescenceThicknessMinimum;
					iridescenceMapper.IridescenceThicknessMaximum = iridescence.iridescenceThicknessMaximum;
					if (iridescence.iridescenceTexture != null)
					{
						var td = await FromTextureInfo(iridescence.iridescenceTexture, false);
						iridescenceMapper.IridescenceTexture = td.Texture;
						iridescenceMapper.IridescenceTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(iridescence.iridescenceTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(iridescence.iridescenceTexture.Index, ext, out var scale, out var offset);
							iridescenceMapper.IridescenceTextureOffset = offset;
							iridescenceMapper.IridescenceTextureScale = scale;
							iridescenceMapper.IridescenceTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) iridescenceMapper.IridescenceTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(iridescence.iridescenceTexture.Index.Value))
						{
							iridescenceMapper.IridescenceTextureScale = new Vector2(1f,-1f);
							iridescenceMapper.IridescenceTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					if (iridescence.iridescenceThicknessTexture != null)
					{
						var td2 = await FromTextureInfo(iridescence.iridescenceThicknessTexture, false);
						iridescenceMapper.IridescenceThicknessTexture = td2.Texture;
						iridescenceMapper.IridescenceThicknessTextureTexCoord = td2.TexCoord;
						var ext2 = GetTextureTransform(iridescence.iridescenceThicknessTexture);
						if (ext2 != null)
						{
							CalculateYOffsetAndScale(iridescence.iridescenceThicknessTexture.Index, ext2, out var scale,
								out var offset);
							iridescenceMapper.IridescenceThicknessTextureOffset = offset;
							iridescenceMapper.IridescenceThicknessTextureScale = scale;
							iridescenceMapper.IridescenceThicknessTextureRotation = td2.Rotation;
							if (td2.TexCoordExtra != null) iridescenceMapper.IridescenceThicknessTextureTexCoord = td2.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(iridescence.iridescenceThicknessTexture.Index.Value))
						{
							iridescenceMapper.IridescenceThicknessTextureScale = new Vector2(1f, -1f);
							iridescenceMapper.IridescenceThicknessTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					mapper.Material.SetKeyword("_IRIDESCENCE", true);
				}
			}

			var specularMapper = mapper as ISpecularMap;
			if (specularMapper != null && KHR_materials_specular)
			{
				var specular = GetSpecular(def);
				if (specular != null)
				{
					specularMapper.SpecularFactor = specular.specularFactor;
					specularMapper.SpecularColorFactor = specular.specularColorFactor.ToUnityColorLinear();
					if (specular.specularTexture != null)
					{
						var td = await FromTextureInfo(specular.specularTexture, false);
						specularMapper.SpecularTexture = td.Texture;
						specularMapper.SpecularTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(specular.specularTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(specular.specularTexture.Index, ext, out var scale,
								out var offset);
							specularMapper.SpecularTextureOffset = offset;
							specularMapper.SpecularTextureScale = scale;
							specularMapper.SpecularTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) specularMapper.SpecularTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(specular.specularTexture.Index.Value))
						{
							specularMapper.SpecularTextureScale = new Vector2(1f, -1f);
							specularMapper.SpecularTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					if (specular.specularColorTexture != null)
					{
						var td2 = await FromTextureInfo(specular.specularColorTexture, false);
						specularMapper.SpecularColorTexture = td2.Texture;
						specularMapper.SpecularColorTextureTexCoord = td2.TexCoord;
						var ext2 = GetTextureTransform(specular.specularColorTexture);
						if (ext2 != null)
						{
							CalculateYOffsetAndScale(specular.specularColorTexture.Index, ext2, out var scale,
								out var offset);
							specularMapper.SpecularColorTextureOffset = offset;
							specularMapper.SpecularColorTextureScale = scale;
							specularMapper.SpecularColorTextureRotation = td2.Rotation;
							if (td2.TexCoordExtra != null) specularMapper.SpecularColorTextureTexCoord = td2.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(specular.specularColorTexture.Index.Value))
						{
							specularMapper.SpecularColorTextureScale = new Vector2(1f, -1f);
							specularMapper.SpecularColorTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					mapper.Material.SetKeyword("_SPECULAR", true);
				}
			}

			var clearcoatMapper = mapper as IClearcoatMap;
			if (clearcoatMapper != null && KHR_materials_clearcoat)
			{
				var clearcoat = GetClearcoat(def);
				if (clearcoat != null)
				{
					clearcoatMapper.ClearcoatFactor = clearcoat.clearcoatFactor;
					clearcoatMapper.ClearcoatRoughnessFactor = clearcoat.clearcoatRoughnessFactor;
					if (clearcoat.clearcoatTexture != null)
					{
						var td = await FromTextureInfo(clearcoat.clearcoatTexture, false);
						clearcoatMapper.ClearcoatTexture = td.Texture;
						clearcoatMapper.ClearcoatTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(clearcoat.clearcoatTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(clearcoat.clearcoatTexture.Index, ext, out var scale,
								out var offset);
							clearcoatMapper.ClearcoatTextureOffset = offset;
							clearcoatMapper.ClearcoatTextureScale = scale;
							clearcoatMapper.ClearcoatTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) clearcoatMapper.ClearcoatTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(clearcoat.clearcoatTexture.Index.Value))
						{
							clearcoatMapper.ClearcoatTextureScale = new Vector2(1f, -1f);
							clearcoatMapper.ClearcoatTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}
					}

					if (clearcoat.clearcoatRoughnessTexture != null)
					{
						var td = await FromTextureInfo(clearcoat.clearcoatRoughnessTexture, false);
						clearcoatMapper.ClearcoatRoughnessTexture = td.Texture;
						clearcoatMapper.ClearcoatRoughnessTextureTexCoord = td.TexCoord;
						var ext = GetTextureTransform(clearcoat.clearcoatRoughnessTexture);
						if (ext != null)
						{
							CalculateYOffsetAndScale(clearcoat.clearcoatRoughnessTexture.Index, ext, out var scale,
								out var offset);
							clearcoatMapper.ClearcoatRoughnessTextureOffset = offset;
							clearcoatMapper.ClearcoatRoughnessTextureScale = scale;
							clearcoatMapper.ClearcoatRoughnessTextureRotation = td.Rotation;
							if (td.TexCoordExtra != null) clearcoatMapper.ClearcoatRoughnessTextureTexCoord = td.TexCoordExtra.Value;
							SetTransformKeyword();
						}
						else if (IsTextureFlipped(clearcoat.clearcoatRoughnessTexture.Index.Value))
						{
							clearcoatMapper.ClearcoatRoughnessTextureScale = new Vector2(1f, -1f);
							clearcoatMapper.ClearcoatRoughnessTextureOffset = new Vector2(0f, 1f);
							SetTransformKeyword();
						}

					}

					var clearcoatNormalMapper = mapper as IClearcoatNormalMap;
					if (clearcoatNormalMapper != null)
					{
						if (clearcoat.clearcoatNormalTexture != null)
						{
							var td = await FromTextureInfo(clearcoat.clearcoatNormalTexture, false);
							
							_runtimeNormalTextures.Add(td.Texture);
							
							clearcoatNormalMapper.ClearcoatNormalTexture = td.Texture;
							clearcoatNormalMapper.ClearcoatNormalTextureTexCoord = td.TexCoord;
							var ext = GetTextureTransform(clearcoat.clearcoatNormalTexture);
							if (ext != null)
							{
								CalculateYOffsetAndScale(clearcoat.clearcoatNormalTexture.Index, ext, out var scale,
									out var offset);
								clearcoatNormalMapper.ClearcoatNormalTextureOffset = offset;
								clearcoatNormalMapper.ClearcoatNormalTextureScale = scale;
								clearcoatNormalMapper.ClearcoatNormalTextureRotation = td.Rotation;
								if (td.TexCoordExtra != null)
									clearcoatNormalMapper.ClearcoatNormalTextureTexCoord = td.TexCoordExtra.Value;
								SetTransformKeyword();
							}
							else if (IsTextureFlipped(clearcoat.clearcoatNormalTexture.Index.Value))
							{
								clearcoatNormalMapper.ClearcoatNormalTextureScale = new Vector2(1f, -1f);
								clearcoatNormalMapper.ClearcoatNormalTextureOffset = new Vector2(0f, 1f);
								SetTransformKeyword();
							}
						}
					}

					mapper.Material.SetKeyword("_CLEARCOAT", true);
				}
			}

			var uniformMapper = mapper as ILitMap;
			if (uniformMapper != null)
			{
				if (def.NormalTexture != null)
				{
					TextureId textureId = def.NormalTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true, true);

					var tex = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.NormalTexture = tex;
					uniformMapper.NormalTexCoord = def.NormalTexture.TexCoord;
					uniformMapper.NormalTexScale = def.NormalTexture.Scale;

					if (tex) _runtimeNormalTextures.Add(tex);
					
					var ext = GetTextureTransform(def.NormalTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						uniformMapper.NormalXOffset = offset;
						uniformMapper.NormalXRotation = ext.Rotation;
						uniformMapper.NormalXScale = scale;
						if (ext.TexCoord != null) uniformMapper.NormalXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						uniformMapper.NormalXScale = new Vector2(1f,-1f);
						uniformMapper.NormalXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}

				if (def.EmissiveTexture != null)
				{
					TextureId textureId = def.EmissiveTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, false, false);
					uniformMapper.EmissiveTexture = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.EmissiveTexCoord = def.EmissiveTexture.TexCoord;

					var ext = GetTextureTransform(def.EmissiveTexture);
					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						uniformMapper.EmissiveXOffset = offset;
						uniformMapper.EmissiveXRotation = ext.Rotation;
						uniformMapper.EmissiveXScale = scale;
						if (ext.TexCoord != null) uniformMapper.EmissiveXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						uniformMapper.EmissiveXScale = new Vector2(1f,-1f);
						uniformMapper.EmissiveXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}

				if (def.OcclusionTexture != null)
				{
					uniformMapper.OcclusionTexStrength = def.OcclusionTexture.Strength;
					TextureId textureId = def.OcclusionTexture.Index;
					await ConstructTexture(textureId.Value, textureId.Id, !KeepCPUCopyOfTexture, true, false);
					uniformMapper.OcclusionTexture = _assetCache.TextureCache[textureId.Id].Texture;
					uniformMapper.OcclusionTexCoord = def.OcclusionTexture.TexCoord;

					var ext = GetTextureTransform(def.OcclusionTexture);

					if (ext != null)
					{
						CalculateYOffsetAndScale(textureId, ext, out var scale, out var offset);
						uniformMapper.OcclusionXOffset = offset;
						uniformMapper.OcclusionXRotation = ext.Rotation;
						uniformMapper.OcclusionXScale = scale;
						if (ext.TexCoord != null) uniformMapper.OcclusionXTexCoord = ext.TexCoord.Value;
						SetTransformKeyword();
					}
					else if (IsTextureFlipped(textureId.Value))
					{
						uniformMapper.OcclusionXScale = new Vector2(1f,-1f);
						uniformMapper.OcclusionXOffset = new Vector2(0f, 1f);
						SetTransformKeyword();
					}
				}

				// Set emissive factor in correct color space
				var emissiveFactor = QualitySettings.activeColorSpace == ColorSpace.Linear ? def.EmissiveFactor.ToUnityColorLinear() : def.EmissiveFactor.ToUnityColorLinear();
				uniformMapper.EmissiveFactor = emissiveFactor;

				var emissiveExt = GetEmissiveStrength(def);
				if (emissiveExt != null && KHR_materials_emissive_strength)
				{
					uniformMapper.EmissiveFactor = uniformMapper.EmissiveFactor * emissiveExt.emissiveStrength;
				}
			}

#if UNITY_EDITOR
			// Restore the mapper if we had to remove it because the shader is broken...
			if (mapper == null) mapper = tempMapper;
#endif
			
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

			foreach (var plugin in Context.Plugins)
			{
				plugin.OnAfterImportMaterial(def, materialIndex, mapper.Material);
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

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_sheen_Factory.EXTENSION_NAME))
			{
				var sheenDef = (KHR_materials_sheen)def.Extensions[KHR_materials_sheen_Factory.EXTENSION_NAME];
				if (sheenDef.sheenColorTexture != null)
				{
					var textureId = sheenDef.sheenColorTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (sheenDef.sheenRoughnessTexture != null)
				{
					var textureId = sheenDef.sheenRoughnessTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}
			
			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_anisotropy_Factory.EXTENSION_NAME))
			{
				var ansiDef = (KHR_materials_anisotropy)def.Extensions[KHR_materials_anisotropy_Factory.EXTENSION_NAME];
				if (ansiDef.anisotropyTexture != null)
				{
					var textureId = ansiDef.anisotropyTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
			}

			if (def.Extensions != null && def.Extensions.ContainsKey(KHR_materials_clearcoat_Factory.EXTENSION_NAME))
			{
				var clearCoatDef = (KHR_materials_clearcoat) def.Extensions[KHR_materials_clearcoat_Factory.EXTENSION_NAME];
				if (clearCoatDef.clearcoatTexture != null)
				{
					var textureId = clearCoatDef.clearcoatTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (clearCoatDef.clearcoatNormalTexture != null)
				{
					var textureId = clearCoatDef.clearcoatNormalTexture.Index;
					tasks.Add(ConstructImageBuffer(textureId.Value, textureId.Id));
				}
				if (clearCoatDef.clearcoatRoughnessTexture != null)
				{
					var textureId = clearCoatDef.clearcoatRoughnessTexture.Index;
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
			return null;
		}

		protected virtual KHR_materials_emissive_strength GetEmissiveStrength(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_emissive_strength_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_emissive_strength) extension;
			}
			return null;
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
		
		protected virtual KHR_materials_sheen GetSheen(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_sheen_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_sheen_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_sheen) extension;
			}
			return null;
		}
		
		protected virtual KHR_materials_anisotropy GetAnisotropy(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_anisotropy_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_anisotropy_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_anisotropy) extension;
			}
			return null;
		}
		
		protected virtual KHR_materials_dispersion GetDispersion(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_dispersion_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_dispersion_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_dispersion) extension;
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

		protected virtual KHR_materials_clearcoat GetClearcoat(GLTFMaterial def)
		{
			if (_gltfRoot.ExtensionsUsed != null && _gltfRoot.ExtensionsUsed.Contains(KHR_materials_clearcoat_Factory.EXTENSION_NAME) &&
			    def.Extensions != null && def.Extensions.TryGetValue(KHR_materials_clearcoat_Factory.EXTENSION_NAME, out var extension))
			{
				return (KHR_materials_clearcoat) extension;
			}
			return null;
		}
	}
}
