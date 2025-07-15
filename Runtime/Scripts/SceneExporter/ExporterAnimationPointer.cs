using System;
using System.Collections.Generic;
using System.Linq;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using GLTF.Utilities;
using UnityEngine;
using UnityGLTF.Extensions;
using UnityGLTF.JsonPointer;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		internal readonly List<IJsonPointerResolver> pointerResolvers = new List<IJsonPointerResolver>();
		private readonly KHR_animation_pointer_Resolver animationPointerResolver = new KHR_animation_pointer_Resolver();

		public void RegisterResolver(IJsonPointerResolver resolver)
		{
			if (!pointerResolvers.Contains(resolver))
				pointerResolvers.Add(resolver);
		}
		
		/// <summary>
		/// AddAnimationData should be called within the <see cref="ExportContext.AfterSceneExport"/> event.
		/// <remarks>
		/// Don't forget to add the GLTFAnimation to the root <see cref="GLTFRoot.Animations"/> list!
		/// <para> e.g.: _exporter.GetRoot().Animations.Add(_animationA); </para>
		/// </remarks>
		/// <para><b>Important:</b> times[] and values[] need to be the same length!</para>
		/// </summary>
		/// <example><code>
		/// var exportContext = new ExportContext(GLTFSettings.GetOrCreateSettings());
		/// var _exporter = new GLTFSceneExporter(new Transform[]{transform }, exportContext);
		/// exportContext.AfterSceneExport += (scene, root) =>
		/// {
		///		GLTFAnimation _animationA = new GLTFAnimation();
		///		_animationA.Name = "TestA";
		///		_exporter.AddAnimationData(rootTransform, "translation", _animationA,
		///			new float[] { 0, 2 },
		///			new object[] { (object)Vector3.one, (object)Vector3.up });
		///		_exporter.GetRoot().Animations.Add(_animationA);
		///	};
		/// </code></example>
		public void AddAnimationData(Object animatedObject, string propertyName, GLTFAnimation animation, float[] times, object[] values)
		{
			if (!animatedObject) return;
			
			// TODO should skip property switches that are not supported without KHR_animation_pointer
			// TODO should probably start with the transform check below and stop afterwards if KHR_animation_pointer is off

			if (values.Length <= 0) return;
			for (var i = 0; i < values.Length; i++)
			{
				if (values[i] != null) continue;

				Debug.LogError(null, $"GLTFExporter error: value {i} in animated property \"{propertyName}\" is null. Skipping", animatedObject);
				return;
			}

			var channelTargetId = GetIndex(animatedObject);
			if (channelTargetId < 0)
			{
				Debug.LogWarning(null, $"Animation for {animatedObject.name} ({animatedObject.GetType()}) has not been exported as the object itself is not exported (disabled/EditorOnly). (InstanceID: {animatedObject.GetInstanceID()})", animatedObject);
				return;
			}

			bool flipValueRange = false;
			float? valueMultiplier = null;
			bool isTextureTransform = false;
			bool keepColorAlpha = true;
			bool convertToLinearColor = false;
			string secondPropertyName = null;
			string extensionName = null;
			var propertyType = values[0]?.GetType();

			var animationPointerExportContext = _plugins.FirstOrDefault(x => x is AnimationPointerExportContext) as AnimationPointerExportContext;
			
			switch (animatedObject)
			{
				case Material material:
					// Debug.Log("material: " + material + ", propertyName: " + propertyName);
					// mapping from known Unity property names to glTF property names
					if (animationPointerExportContext == null ||
					    animationPointerExportContext.materialPropertiesRemapper == null)
					{
						if (animationPointerExportContext == null)
							Debug.Log(LogType.Error, "No AnimationPointerExportContext found in GLTFSceneExporter. Skipping animation");
						else
							Debug.Log(LogType.Error, "No MaterialPropertiesRemapper found in AnimationPointerExportContext. Skipping animation");
						return;
					}
					
					if (!animationPointerExportContext.materialPropertiesRemapper.GetMapFromUnityMaterial(material, propertyName, out MaterialPointerPropertyMap map))
					{
						break;
					}
					
					secondPropertyName = map.GltfSecondaryPropertyName;
					propertyName = map.GltfPropertyName;
					extensionName = map.ExtensionName;
					
					flipValueRange = map.ExportFlipValueRange;
					valueMultiplier = map.ExportValueMultiplier == 1f ? null : (float?) map.ExportValueMultiplier;
					isTextureTransform = map.PropertyType == MaterialPointerPropertyMap.PropertyTypeOption.TextureTransform;
					keepColorAlpha = map.ExportKeepColorAlpha;
					convertToLinearColor = map.ExportConvertToLinearColor;
					
					break;
				case Light light:
					extensionName = KHR_lights_punctualExtensionFactory.EXTENSION_NAME;
					switch (propertyName)
					{
						case "m_Color":
							propertyName = $"color";
							keepColorAlpha = false;
							convertToLinearColor = true;
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
							valueMultiplier = Mathf.Deg2Rad / 2 * 0.8f;
							propertyName = $"spot/innerConeAngle";
							break;
						case "m_Range":
							propertyName = "range";
							break;
						default:
							extensionName = null;
							Debug.Log(LogType.Warning, "Unknown property name on Light " + light + ": " + propertyName);
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
							default:
								Debug.Log(LogType.Warning, "Unknown property name on Camera " + camera + ": " + propertyName);
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
								valueMultiplier = Mathf.Deg2Rad;
								break;
							case "near clip plane":
								propertyName = "perspective/znear";
								break;
							case "far clip plane":
								propertyName = "perspective/zfar";
								break;
							case "backgroundColor":
								convertToLinearColor = true;
								break;
							default:
								Debug.Log(LogType.Warning, "Unknown property name on Camera " + camera + ": " + propertyName);
								break;
						}
					}
					break;
				case SkinnedMeshRenderer skinnedMesh:
					// this code is adapted from SkinnedMeshRendererEditor (which calculates the right range for sliders to show)
					// instead of calculating per blend shape, we're assuming all blendshapes have the same min/max here though.
					var minBlendShapeFrameWeight = 0.0f;
					var maxBlendShapeFrameWeight = 0.0f;

					var sharedMesh = skinnedMesh.sharedMesh;
					if (!sharedMesh)
					{
						Debug.Log(LogType.Error, "No mesh on SkinnedMeshRenderer " + skinnedMesh + ", skipping animation");
						return;
					}

					var shapeCount = sharedMesh.blendShapeCount;
					for (int index = 0; index < shapeCount; ++index)
					{
						var blendShapeFrameCount = sharedMesh.GetBlendShapeFrameCount(index);
						for (var frameIndex = 0; frameIndex < blendShapeFrameCount; ++frameIndex)
						{
							var shapeFrameWeight = sharedMesh.GetBlendShapeFrameWeight(index, frameIndex);
							minBlendShapeFrameWeight = Mathf.Min(shapeFrameWeight, minBlendShapeFrameWeight);
							maxBlendShapeFrameWeight = Mathf.Max(shapeFrameWeight, maxBlendShapeFrameWeight);
						}
					}

					if (maxBlendShapeFrameWeight != 0)
						valueMultiplier = 1.0f / maxBlendShapeFrameWeight;

					break;
				case Transform _:
					// generally allowed and already in the right format
					break;
				default:
					// propertyName is exported as-is
					// Debug.LogWarning($"Implicitly handling animated property \"{propertyName}\" for target {animatedObject}", animatedObject);

					// filtering for what to include / what not to include based on whether its target can be resolved
					if (UseAnimationPointer && animatedObject is Component _)
					{
						var couldResolve = false;
						var prop = $"/nodes/{channelTargetId}/{propertyName}";
						foreach (var res in pointerResolvers)
						{
							// TODO: ideally we have a new method here to just ask the resolver if it supports that type
							if (res.TryResolve(animatedObject, ref prop))
							{
								couldResolve = true;
							}
						}
						if (!couldResolve)
						{
							return;
						}
						
						// If the animated property is a color, we need to convert to linear –
						// we're doing the same on regular non-animated property export.
						if (propertyType == typeof(Color))
							convertToLinearColor = true;
					}
					break;
			}

			var Node = new NodeId
			{
				Id = channelTargetId,
				Root = _root
			};

			AccessorId timeAccessor = ExportAccessor(times);

			AnimationChannel Tchannel = new AnimationChannel();
			AnimationChannelTarget TchannelTarget = new AnimationChannelTarget() { Path = propertyName, Node = Node };
			Tchannel.Target = TchannelTarget;

			AnimationSampler Tsampler = new AnimationSampler();
			Tsampler.Input = timeAccessor;

			// for cases where one property needs to be split up into multiple tracks
			// example: emissiveFactor * emissiveStrength
			// TODO not needed when secondPropertyName==null
			AnimationChannel Tchannel2 = new AnimationChannel();
			AnimationChannelTarget TchannelTarget2 = new AnimationChannelTarget() { Path = secondPropertyName, Node = Node  };
			Tchannel2.Target = TchannelTarget2;
			AnimationSampler Tsampler2 = new AnimationSampler();
			Tsampler2.Input = timeAccessor;
			bool actuallyNeedSecondSampler = true;

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
				case float[] _:
					// check that all entries have the same length using a for loop
					var firstLength = ((float[])values[0]).Length;
					for (var i = 1; i < values.Length; i++)
					{
						if (((float[])values[i]).Length == firstLength) continue;

						Debug.Log(LogType.Error, "When animating float arrays, all array entries must have the same length. Skipping");
						return;
					}

					// construct a float array of all the float arrays together
					var floatArray = new float[values.Length * firstLength];

					// copy the individual arrays into the float array using Array.Copy
					for (int i = 0; i < values.Length; i++)
						Array.Copy((float[])values[i], 0, floatArray, i * firstLength, firstLength);

					// glTF weights 0..1 match to Unity weights 0..100, but Unity weights can be in arbitrary ranges
					if (valueMultiplier.HasValue)
					{
						for (var i = 0; i < floatArray.Length; i++)
							floatArray[i] *= valueMultiplier.Value;
					}

					Tsampler.Output = ExportAccessor(floatArray);
					break;
				case Vector2 _:
					Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e => (Vector2)e));
					break;
				case Vector3 _:
					if (propertyName == "translation")
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e =>
						{
							var v = (Vector3)e;
							v.Scale(new Vector3(-1, 1, 1));
							return v;
						}));
					else
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
				case Quaternion _:
					var animatedNode = _root.Nodes[channelTargetId];
					var needsFlippedLookDirection = animatedNode.Light != null || animatedNode.Camera != null;
					Tsampler.Output = ExportAccessorSwitchHandedness(Array.ConvertAll(values, e => (Quaternion)e), needsFlippedLookDirection); // Vec4 for rotations
					break;
				case Color _:
					if (propertyName == "emissiveFactor" && secondPropertyName != null)
					{
						var colors = new Color[values.Length];
						var strengths = new float[values.Length];
						actuallyNeedSecondSampler = false;
						var pluginSettings = (_plugins.FirstOrDefault(x => x is MaterialExtensionsExportContext) as MaterialExtensionsExportContext)?.settings;
						var emissiveStrengthSupported = pluginSettings != null && pluginSettings.KHR_materials_emissive_strength;
						if (emissiveStrengthSupported)
						{
							for (int i = 0; i < values.Length; i++)
							{
								DecomposeEmissionColor((Color) values[i], out var color, out var intensity);
								colors[i] = color;
								strengths[i] = intensity;
								if (intensity > 1)
									actuallyNeedSecondSampler = true;
							}
						}
						else
						{
							// clamp 0..1
							for (int i = 0; i < values.Length; i++)
							{
								var c = (Color) values[i];
								if (c.r > 1) c.r = 1;
								if (c.g > 1) c.g = 1;
								if (c.b > 1) c.b = 1;
								colors[i] = c;
							}
						}
						
						Tsampler.Output = ExportAccessor(colors, false);
						if (emissiveStrengthSupported)
							Tsampler2.Output = ExportAccessor(strengths);
					}
					else
					{
						Tsampler.Output = ExportAccessor(Array.ConvertAll(values, e =>
						{
							var c = (Color) e;
							if (convertToLinearColor)
								c = c.linear;
							return c;
						}), keepColorAlpha);
					}
					break;
			}

			if (Tsampler.Output  != null) Tsampler.Output.Value.BufferView.Value.ByteStride  = 0;
			if (Tsampler2.Output != null) Tsampler2.Output.Value.BufferView.Value.ByteStride = 0;

			if (Tsampler.Output == null)
			{
				Debug.LogError($"GLTFExporter: Something went wrong, empty sampler output for \"{propertyName}\" in {animatedObject}", animatedObject);
				return;
			}

			Tchannel.Sampler = new AnimationSamplerId
			{
				Id = animation.Samplers.Count,
				GLTFAnimation = animation,
				Root = _root
			};
			animation.Samplers.Add(Tsampler);
			animation.Channels.Add(Tchannel);

			if (UseAnimationPointer)
				ConvertToAnimationPointer(animatedObject, propertyName, TchannelTarget);

			// in some cases, extensions aren't required even when we think they might, e.g. for emission color animation.
			// if all animated values are below 1, we don't need a separate channel for emissive_intensity.
			if (!actuallyNeedSecondSampler)
			{
				extensionName = null;
				secondPropertyName = null;
			}

			if (extensionName != null)
			{
				DeclareExtensionUsage(extensionName, false);

				// add extension to material if needed
				if(animatedObject is Material material)
				{
					var mat = GetMaterialId(_root, material);
					if (mat.Value.Extensions == null || !mat.Value.Extensions.ContainsKey(extensionName))
					{
						mat.Value.AddExtension(extensionName, GLTFProperty.CreateEmptyExtension(extensionName));
					}
				}
			}

			if (secondPropertyName != null)
			{
				Tchannel2.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};
				animation.Samplers.Add(Tsampler2);
				animation.Channels.Add(Tchannel2);

				if (UseAnimationPointer)
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
