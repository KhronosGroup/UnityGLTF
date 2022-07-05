using System;
using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;
using UnityGLTF.JsonPointer;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		internal readonly List<IJsonPointerResolver> pointerResolvers = new List<IJsonPointerResolver>();
		private KHR_animation_pointer_Resolver animationPointerResolver = new KHR_animation_pointer_Resolver();

		public void RegisterResolver(IJsonPointerResolver resolver)
		{
			if(!this.pointerResolvers.Contains(resolver))
				this.pointerResolvers.Add(resolver);
		}

		public void AddAnimationData(Object animatedObject, string propertyName, GLTFAnimation animation, float[] times, object[] values)
		{
			if (!animatedObject) return;

			if (!settings.UseAnimationPointer)
			{
				Debug.LogWarning("Trying to export arbitrary animation (" + propertyName + ") - this requires KHR_animation_pointer", animatedObject);
				return;
			}
			if (values.Length <= 0) return;

			var channelTargetId = GetAnimationTargetId(animatedObject);
			if (channelTargetId < 0)
			{
				Debug.LogWarning($"An animated {animatedObject.GetType()} has not been exported, is the object disabled? {animatedObject.name} (InstanceID: {animatedObject.GetInstanceID()})", animatedObject);
				return;
			}

			bool flipValueRange = false;
			float? valueMultiplier = null;
			bool isTextureTransform = false;
			string secondPropertyName = null;

			switch (animatedObject)
			{
				case Material material:
					// Debug.Log("material: " + material + ", propertyName: " + propertyName);
					// mapping from known Unity property names to glTF property names
					switch (propertyName)
					{
						case "_Color":
						case "_BaseColor":
						case "_BaseColorFactor":
						case "baseColorFactor":
							propertyName = "pbrMetallicRoughness/baseColorFactor";
							break;
						case "_Smoothness":
						case "_Glossiness":
							propertyName = "pbrMetallicRoughness/roughnessFactor";
							flipValueRange = true;
							break;
						case "_Roughness":
						case "_RoughnessFactor":
						case "roughnessFactor":
							propertyName = "pbrMetallicRoughness/roughnessFactor";
							break;
						case "_Metallic":
						case "_MetallicFactor":
						case "metallicFactor":
							propertyName = "pbrMetallicRoughness/metallicFactor";
							break;
						case "_MainTex_ST":
						case "_BaseMap_ST":
						case "_BaseColorTexture_ST":
						case "baseColorTexture_ST":
							if (!(material.HasProperty("_MainTex") && material.GetTexture("_MainTex")) &&
							    !(material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap")) &&
							    !(material.HasProperty("_BaseColorTexture") && material.GetTexture("_BaseColorTexture")) &&
							    !(material.HasProperty("baseColorTexture") && material.GetTexture("baseColorTexture"))) return;
							propertyName = $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"pbrMetallicRoughness/baseColorTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;
						case "_EmissionColor":
						case "_EmissiveFactor":
						case "emissiveFactor":
							propertyName = "emissiveFactor";
							secondPropertyName = $"extensions/{KHR_materials_emissive_strength_Factory.EXTENSION_NAME}/{nameof(KHR_materials_emissive_strength.emissiveStrength)}";
							break;
						case "_EmissionMap_ST":
						case "_EmissiveTexture_ST":
						case "emissiveTexture_ST":
							if (!(material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap")) &&
							    !(material.HasProperty("_EmissiveTexture") && material.GetTexture("_EmissiveTexture")) &&
							    !(material.HasProperty("emissiveTexture") && material.GetTexture("emissiveTexture"))) return;
							propertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"emissiveTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;
						case "_Cutoff":
						case "_AlphaCutoff":
						case "alphaCutoff":
							propertyName = "alphaCutoff";
							break;
						case "_BumpScale":
						case "_NormalScale":
						case "normalScale":
							propertyName = "normalTexture/scale";
							break;
						case "_BumpMap_ST":
						case "_NormalTexture_ST":
						case "normalTexture_ST":
							if (!(material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap")) &&
							    !(material.HasProperty("_NormalTexture") && material.GetTexture("_NormalTexture")) &&
							    !(material.HasProperty("normalTexture") && material.GetTexture("normalTexture"))) return;
							propertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"normalTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;
						case "_OcclusionStrength":
						case "occlusionStrength":
							propertyName = "occlusionTexture/strength";
							break;
						case "_OcclusionMap_ST":
						case "_OcclusionTexture_ST":
						case "occlusionTexture_ST":
							if (!(material.HasProperty("_OcclusionMap") && material.GetTexture("_OcclusionMap")) &&
							    !(material.HasProperty("_OcclusionTexture") && material.GetTexture("_OcclusionTexture")) &&
							    !(material.HasProperty("occlusionTexture") && material.GetTexture("occlusionTexture"))) return;
							propertyName = $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.SCALE}";
							secondPropertyName = $"occlusionTexture/extensions/{ExtTextureTransformExtensionFactory.EXTENSION_NAME}/{ExtTextureTransformExtensionFactory.OFFSET}";
							isTextureTransform = true;
							break;

						// TODO metallic/roughness _ST

						// KHR_materials_transmission
						case "_TransmissionFactor":
						case "transmissionFactor":
							propertyName = $"extensions/{KHR_materials_transmission_Factory.EXTENSION_NAME}/{nameof(KHR_materials_transmission.transmissionFactor)}";
							break;

						// KHR_materials_volume
						case "_ThicknessFactor":
						case "thicknessFactor":
							propertyName = $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.thicknessFactor)}";
							break;
						case "_AttenuationDistance":
						case "attenuationDistance":
							propertyName = $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationDistance)}";
							break;
						case "_AttenuationColor":
						case "attenuationColor":
							propertyName = $"extensions/{KHR_materials_volume_Factory.EXTENSION_NAME}/{nameof(KHR_materials_volume.attenuationColor)}";
							break;

						// KHR_materials_ior
						case "_IOR":
						case "ior":
							propertyName = $"extensions/{KHR_materials_ior_Factory.EXTENSION_NAME}/{nameof(KHR_materials_ior.ior)}";
							break;

						// KHR_materials_iridescence
						case "_IridescenceFactor":
						case "iridescenceFactor":
							propertyName = $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceFactor)}";
							break;
						case "_IridescenceIor":
						case "iridescenceIor":
							propertyName = $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceIor)}";
							break;
						case "_IridescenceThicknessMinimum":
						case "iridescenceThicknessMinimum":
							propertyName = $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceThicknessMinimum)}";
							break;
						case "_IridescenceThicknessMaximum":
						case "iridescenceThicknessMaximum":
							propertyName = $"extensions/{KHR_materials_iridescence_Factory.EXTENSION_NAME}/{nameof(KHR_materials_iridescence.iridescenceThicknessMaximum)}";
							break;

						// KHR_materials_specular
						case "_SpecularFactor":
						case "specularFactor":
							propertyName = $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularFactor)}";
							break;
						case "_SpecularColorFactor":
						case "specularColorFactor":
							propertyName = $"extensions/{KHR_materials_specular_Factory.EXTENSION_NAME}/{nameof(KHR_materials_specular.specularColorFactor)}";
							break;

						// TODO KHR_materials_clearcoat
						// case "_ClearcoatFactor":
						// case "clearcoatFactor":
						// 	propertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatFactor)}";
						// 	break;
						// case "_ClearcoatRoughnessFactor":
						// case "clearcoatRoughnessFactor":
						// 	propertyName = $"extensions/{KHR_materials_clearcoat_Factory.EXTENSION_NAME}/{nameof(KHR_materials_clearcoat.clearcoatRoughnessFactor)}";
						// 	break;

						// TODO KHR_materials_sheen
						// case "_SheenColorFactor":
						// case "sheenColorFactor":
						// 	propertyName = $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenColorFactor)}";
						// 	break;
						// case "_SheenRoughnessFactor":
						// case "sheenRoughnessFactor":
						// 	propertyName = $"extensions/{KHR_materials_sheen_Factory.EXTENSION_NAME}/{nameof(KHR_materials_sheen.sheenRoughnessFactor)}";
						// 	break;
					}
					break;
				case Light light:
					switch (propertyName)
					{
						case "m_Color":
							propertyName = $"color";
							break;
						case "m_Intensity":
							valueMultiplier = Mathf.PI; // matches ExportLight
							propertyName = $"intensity";
							break;
						case "m_SpotAngle":
							valueMultiplier = Mathf.Deg2Rad / 2;
							propertyName = $"spot/outerConeAngle";
							break;
						case "m_InnerSpotAngle":
							valueMultiplier = Mathf.Deg2Rad / 2;
							propertyName = $"spot/innerConeAngle";
							break;
						case "m_Range":
							propertyName = "range";
							break;
					}
					break;
				case Camera camera:
					if(camera.orthographic)
					{
						switch (propertyName)
						{
							case "orthographic size":
								// TODO conversion factor
								propertyName = "orthographic/ymag";
								secondPropertyName = "orthographic/xmag";
								break;
							case "near clip plane":
								propertyName = "orthographic/znear";
								break;
							case "far clip plane":
								propertyName = "orthographic/zfar";
								break;
						}
					}
					else
					{
						switch (propertyName)
						{
							case "field of view":
								// TODO conversion factor
								propertyName = "perspective/yfov";
								break;
							case "near clip plane":
								propertyName = "perspective/znear";
								break;
							case "far clip plane":
								propertyName = "perspective/zfar";
								break;
						}
					}
					break;
				default:
					Debug.LogWarning($"Implicitly handling animated property \"{propertyName}\" for target {animatedObject}", animatedObject);
					break;
			}

			AccessorId timeAccessor = ExportAccessor(times);

			AnimationChannel Tchannel = new AnimationChannel();
			AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
			Tchannel.Target = TchannelTarget;

			AnimationSampler Tsampler = new AnimationSampler();
			Tsampler.Input = timeAccessor;

			// for cases where one property needs to be split up into multiple tracks
			// example: emissiveFactor * emissiveStrength
			// TODO not needed when secondPropertyName==null
			AnimationChannel Tchannel2 = new AnimationChannel();
			AnimationChannelTarget TchannelTarget2 = new AnimationChannelTarget();
			Tchannel2.Target = TchannelTarget2;
			AnimationSampler Tsampler2 = new AnimationSampler();
			Tsampler2.Input = timeAccessor;

			var val = values[0];
			switch (val)
			{
				case float _:
					if (flipValueRange)
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => 1.0f - (float)e));
					}
					else if (valueMultiplier.HasValue)
					{
						var multiplier = valueMultiplier.Value;
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => ((float)e) * multiplier));
					}
					else
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (float)e));

						if (propertyName == "orthographic/ymag")
						{
							Tsampler2.Output = Tsampler.Output;
						}
					}
					break;
				case Vector2 _:
					Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector2)e));
					break;
				case Vector3 _:
					Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector3)e));
					break;
				case Vector4 _:
					if (!isTextureTransform)
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector4)e));
					}
					else
					{
						var scales = new Vector2[values.Length];
						var offsets = new Vector2[values.Length];
						for (int i = 0; i < values.Length; i++)
						{
							DecomposeScaleOffset((Vector4) values[i], out var scale, out var offset);
							scales[i] = scale;
							offsets[i] = offset;
						}
						Tsampler.Output = ExportAccessor(scales);
						Tsampler2.Output = ExportAccessor(offsets);
					}
					break;
				case Color _:
					if (propertyName == "emissiveFactor" && secondPropertyName != null)
					{
						var colors = new Color[values.Length];
						var strengths = new float[values.Length];
						for (int i = 0; i < values.Length; i++)
						{
							DecomposeEmissionColor((Color) values[i], out var color, out var intensity);
							colors[i] = color;
							strengths[i] = intensity;
						}
						Tsampler.Output = ExportAccessor(colors);
						Tsampler2.Output = ExportAccessor(strengths);
					}
					else
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Color)e));
					}
					break;
			}

			Tchannel.Sampler = new AnimationSamplerId
			{
				Id = animation.Samplers.Count,
				GLTFAnimation = animation,
				Root = _root
			};
			animation.Samplers.Add(Tsampler);
			animation.Channels.Add(Tchannel);

			ConvertToAnimationPointer(animatedObject, propertyName, TchannelTarget);

			if(secondPropertyName != null)
			{
				Tchannel2.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};
				animation.Samplers.Add(Tsampler2);
				animation.Channels.Add(Tchannel2);

				ConvertToAnimationPointer(animatedObject, secondPropertyName, TchannelTarget2);
			}
		}

		void ConvertToAnimationPointer(object animatedObject, string propertyName, AnimationChannelTarget target)
		{
			var ext = new KHR_animation_pointer();
			ext.propertyBinding = propertyName;
			ext.animatedObject = animatedObject;
			ext.channel = target;
			animationPointerResolver.Add(ext);

			target.Node = null;
			target.Path = "pointer";
			target.AddExtension(KHR_animation_pointer.EXTENSION_NAME, ext);
			DeclareExtensionUsage(KHR_animation_pointer.EXTENSION_NAME, false);
		}
	}
}
