using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
using GLTF.Schema.KHR_lights_punctual;
using GLTF.Utilities;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;

namespace UnityGLTF
{
	public partial class GLTFSceneImporter
	{
#if UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER
		private static string RelativePathFrom(Transform self, Transform root)
		{
			var path = new List<String>();
			for (var current = self; current != null; current = current.parent)
			{
				if (current == root)
				{
					return String.Join("/", path.ToArray());
				}

				path.Insert(0, current.name);
			}

			throw new Exception("no RelativePath");
		}

		protected virtual async Task BuildAnimationSamplers(GLTFAnimation animation, int animationId)
		{
			// look up expected data types
			var typeMap = new Dictionary<int, string>();
			foreach (var channel in animation.Channels)
			{
				typeMap[channel.Sampler.Id] = channel.Target.Path.ToString();
			}

			var samplers = _assetCache.AnimationCache[animationId].Samplers;
			var samplersByType = new Dictionary<string, List<AttributeAccessor>>
			{
				{"time", new List<AttributeAccessor>(animation.Samplers.Count)}
			};

			for (var i = 0; i < animation.Samplers.Count; i++)
			{
				// no sense generating unused samplers
				if (!typeMap.ContainsKey(i))
				{
					continue;
				}

				var samplerDef = animation.Samplers[i];

				samplers[i].Interpolation = samplerDef.Interpolation;

				// set up input accessors
				BufferCacheData inputBufferCacheData = await GetBufferData(samplerDef.Input.Value.BufferView.Value.Buffer);
				AttributeAccessor attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Input,
					bufferData = inputBufferCacheData.bufferData,
					Offset = inputBufferCacheData.ChunkOffset
				};

				samplers[i].Input = attributeAccessor;
				samplersByType["time"].Add(attributeAccessor);

				// set up output accessors
				BufferCacheData outputBufferCacheData = await GetBufferData(samplerDef.Output.Value.BufferView.Value.Buffer);
				attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Output,
					bufferData = outputBufferCacheData.bufferData,
					Offset = outputBufferCacheData.ChunkOffset
				};

				samplers[i].Output = attributeAccessor;

				if (!samplersByType.ContainsKey(typeMap[i]))
				{
					samplersByType[typeMap[i]] = new List<AttributeAccessor>();
				}

				samplersByType[typeMap[i]].Add(attributeAccessor);
			}

			// populate attributeAccessors with buffer data
			GLTFHelpers.BuildAnimationSamplers(ref samplersByType);
		}

		protected void SetAnimationCurve(
			AnimationClip clip,
			string relativePath,
			string[] propertyNames,
			NumericArray input,
			NumericArray output,
			InterpolationType mode,
			Type curveType,
			ValuesConvertion getConvertedValues)
		{
			var channelCount = propertyNames.Length;
			var frameCount = input.AsFloats.Length;

			// copy all the key frame data to cache
			Keyframe[][] keyframes = new Keyframe[channelCount][];
			for (var ci = 0; ci < channelCount; ++ci)
			{
				keyframes[ci] = new Keyframe[frameCount];
			}

			for (var i = 0; i < frameCount; ++i)
			{
				var time = input.AsFloats[i];

				float[] values = null;
				float[] inTangents = null;
				float[] outTangents = null;
				if (mode == InterpolationType.CUBICSPLINE)
				{
					// For cubic spline, the output will contain 3 values per keyframe; inTangent, dataPoint, and outTangent.
					// https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#appendix-c-spline-interpolation

					var cubicIndex = i * 3;
					inTangents = getConvertedValues(output, cubicIndex);
					values = getConvertedValues(output, cubicIndex + 1);
					outTangents = getConvertedValues(output, cubicIndex + 2);
				}
				else
				{
					// For other interpolation types, the output will only contain one value per keyframe
					values = getConvertedValues(output, i);
				}

				for (var ci = 0; ci < channelCount; ++ci)
				{
					if (mode == InterpolationType.CUBICSPLINE)
					{
						keyframes[ci][i] = new Keyframe(time, values[ci], inTangents[ci], outTangents[ci]);
					}
					else
					{
						keyframes[ci][i] = new Keyframe(time, values[ci]);
					}
				}
			}

			if (mode == InterpolationType.LINEAR && channelCount == 4 && propertyNames.All(p => p == "localRotation.x" || p == "localRotation.y" || p == "localRotation.z" || p == "localRotation.w"))
			{
				Quaternion prev = Quaternion.identity;
				for (int i = 0; i < keyframes[0].Length; i++)
				{
					Quaternion q = new Quaternion(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value, keyframes[3][i].value);
					if (i > 0)
					{
						if (Quaternion.Dot(prev, q) < 0)
							q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
						
						keyframes[0][i].value = q.x;
						keyframes[1][i].value = q.y;
						keyframes[2][i].value = q.z;
						keyframes[3][i].value = q.w;
					}
					prev = q;
				}
			}
			
			for (var ci = 0; ci < channelCount; ci++)
			{
				if (mode != InterpolationType.CUBICSPLINE)
				{
					// For cubic spline interpolation, the inTangents and outTangents are already explicitly defined.
					// For the rest, set them appropriately.
					for (var i = 0; i < keyframes[ci].Length; i++)
					{
						SetTangentMode(keyframes[ci], i, mode);
					}
				}

				// copy all key frames data to animation curve and add it to the clip
				AnimationCurve curve = new AnimationCurve(keyframes[ci]);
				
#if !UNITY_EDITOR
				// Only in editor SetCurve works with non-legacy clips
				if (clip.legacy)
#endif 
					clip.SetCurve(relativePath, curveType, propertyNames[ci], curve);
			}
		}

		private void SetTangentMode(Keyframe[] keyframes, int keyframeIndex, InterpolationType interpolation)
		{
			var key = keyframes[keyframeIndex];

			switch (interpolation)
			{
				case InterpolationType.CATMULLROMSPLINE:
					key.inTangent = 0;
					key.outTangent = 0;
					break;
				case InterpolationType.LINEAR:
					key.inTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex);
					key.outTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex + 1);
					break;
				case InterpolationType.STEP:
					key.inTangent = float.PositiveInfinity;
					key.outTangent = 0;
					break;
				default:
					throw new NotImplementedException($"Unknown interpolation type for animation (File: {_gltfFileName})");
			}
			keyframes[keyframeIndex] = key;
		}

		private static float GetCurveKeyframeLeftLinearSlope(Keyframe[] keyframes, int keyframeIndex)
		{
			if (keyframeIndex <= 0 || keyframeIndex >= keyframes.Length)
			{
				return 0;
			}

			var valueDelta = keyframes[keyframeIndex].value - keyframes[keyframeIndex - 1].value;
			var timeDelta = keyframes[keyframeIndex].time - keyframes[keyframeIndex - 1].time;

			if (timeDelta <= 0)
			{
				var k = keyframes[keyframeIndex];
				k.time = keyframes[keyframeIndex - 1].time + Mathf.Epsilon + 1 / 100f;
				keyframes[keyframeIndex] = k;
				Debug.Log(LogType.Warning, "Time of subsequent animation keyframes is not increasing (glTF-Validator error ACCESSOR_ANIMATION_INPUT_NON_INCREASING)");
				return float.PositiveInfinity;
			}
			return valueDelta / timeDelta;
		}

		protected async Task<AnimationClip> ConstructClip(Transform root, int animationId, CancellationToken cancellationToken)
		{
			GLTFAnimation animation = _gltfRoot.Animations[animationId];

			AnimationCacheData animationCache = _assetCache.AnimationCache[animationId];
			if (animationCache == null)
			{
				animationCache = new AnimationCacheData(animation.Samplers.Count);
				_assetCache.AnimationCache[animationId] = animationCache;
			}
			else if (animationCache.LoadedAnimationClip != null)
			{
				return animationCache.LoadedAnimationClip;
			}

			// unpack accessors
			await BuildAnimationSamplers(animation, animationId);

			// init clip
			AnimationClip clip = new AnimationClip
			{
				name = animation.Name ?? $"animation:{animationId}",
			};
			_assetCache.AnimationCache[animationId].LoadedAnimationClip = clip;

			if (_options.AnimationMethod == AnimationMethod.Legacy)
				clip.legacy = true;

			int nodeId = -1;
			
			foreach (AnimationChannel channel in animation.Channels)
			{
				bool usesPointer = false;
				IExtension pointerExtension = null;
				AnimationSamplerCacheData samplerCache = animationCache.Samplers[channel.Sampler.Id];
				if (channel.Target.Extensions != null && channel.Target.Extensions.TryGetValue(
					    KHR_animation_pointer.EXTENSION_NAME,
					    out pointerExtension))
				{
					if (Context.TryGetPlugin<AnimationPointerImportContext>(out _))
						usesPointer = true;
				}
				
				if (!usesPointer && channel.Target.Node == null)
				{
					// If a channel doesn't have a target node, then just skip it.
					// This is legal and is present in one of the asset generator models, but means that animation doesn't actually do anything.
					// https://github.com/KhronosGroup/glTF-Asset-Generator/tree/master/Output/Positive/Animation_NodeMisc
					// Model 08
					continue;
				}

				string relativePath = null;
				AnimationPointerData pointerData = null;
				string[] propertyNames;
				GLTFAnimationChannelPath path = GLTFAnimationChannelPath.translation;
				
				if (usesPointer && pointerExtension != null)
				{
					KHR_animation_pointer pointer = pointerExtension as KHR_animation_pointer;
					if (pointer == null || pointer.path == null)
						continue;

					path = GLTFAnimationChannelPath.pointer;
					relativePath = pointer.path;
					var pointerHierarchy = AnimationPointerPathHierarchy.CreateHierarchyFromFullPath(relativePath);

					if (pointerHierarchy.elementType == AnimationPointerPathHierarchy.ElementTypeOptions.Extension)
					{
						if (!_gltfRoot.Extensions.TryGetValue(pointerHierarchy.elementName, out IExtension hierarchyExtension))
							continue;
						
						if (hierarchyExtension is IAnimationPointerRootExtension rootExtension)
						{
							if (rootExtension.TryGetAnimationPointerData(_gltfRoot, pointerHierarchy, out pointerData))
								nodeId = pointerData.nodeId;
							else
								continue;
						}
					}
					else
					if (pointerHierarchy.elementType == AnimationPointerPathHierarchy.ElementTypeOptions.Root)
					{
						var rootType = pointerHierarchy.elementName;
						var rootIndex = pointerHierarchy.FindElement(AnimationPointerPathHierarchy.ElementTypeOptions.Index);
						if (rootIndex == null)
							continue;
						
						switch (rootType)
						{
							case "nodes":
								var pointerPropertyElement = pointerHierarchy.FindElement(AnimationPointerPathHierarchy.ElementTypeOptions.Property);
								if (pointerPropertyElement == null)
									continue;
								
								pointerData = new AnimationPointerData();
								pointerData.nodeId = rootIndex.index;
								nodeId = rootIndex.index;
								switch (pointerPropertyElement.elementName)
								{
									case "translation":
										path = GLTFAnimationChannelPath.translation;
										break;
									case "rotation":
										path = GLTFAnimationChannelPath.rotation;
										break;
									case "scale":
										path = GLTFAnimationChannelPath.scale;
										break;
									case "weights":
										path = GLTFAnimationChannelPath.weights;
										break;
								}

								break; 
							//case "materials":
							//case "cameras":
								//nodeId = _gltfRoot.Cameras[rootIndex]. pointerHierarchy.index;
								//break;
							default:
								continue;
								//throw new NotImplementedException();
						}
					}
					else
						continue;
				}
				else
				{
					if (channel.Target == null || channel.Target.Node == null)
						continue;
					nodeId = channel.Target.Node.Id;
				}
				
				var node = await GetNode(nodeId, cancellationToken);
				var targetNode = _gltfRoot.Nodes[nodeId];
				relativePath = RelativePathFrom(node.transform, root);
				NumericArray input = samplerCache.Input.AccessorContent;
				NumericArray output = samplerCache.Output.AccessorContent;
				
				if (!usesPointer)
				{
					var known = Enum.TryParse(channel.Target.Path, out path);
					if (!known) continue;
				}
				
				switch (path)
				{
					case GLTFAnimationChannelPath.pointer:
						if (pointerData == null)
							continue;
						if (pointerData.conversion == null)
							continue;
						propertyNames = pointerData.unityProperties;
						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, pointerData.animationType,
							(data, frame) => pointerData.conversion(data, frame));
						break;
					case GLTFAnimationChannelPath.translation:
						propertyNames = new string[] { "localPosition.x", "localPosition.y", "localPosition.z" };
#if UNITY_EDITOR
						// TODO technically this should be postprocessing in the ScriptedImporter instead,
						// but performance is much better if we do it when constructing the clips
						var factor = Context?.ImportScaleFactor ?? 1f;
#endif
						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var position = data.AsFloat3s[frame].ToUnityVector3Convert();
#if UNITY_EDITOR
											  return new float[] { position.x * factor, position.y * factor, position.z * factor};
#else
											  return new float[] { position.x, position.y, position.z };
#endif
										  });
						break;

					case GLTFAnimationChannelPath.rotation:
						propertyNames = new string[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
						bool hasLight = (targetNode.Extensions != null &&
						                 targetNode.Extensions.ContainsKey(KHR_lights_punctualExtensionFactory
							                 .EXTENSION_NAME) && Context.TryGetPlugin<LightsPunctualImportContext>(out _));
						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var rotation = data.AsFloat4s[frame];
											  var quaternion = rotation.ToUnityQuaternionConvert();
											  if (hasLight)
											  {
												  quaternion *= Quaternion.Euler(0,180, 0);
											  }
											  return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
										  });

						break;

					case GLTFAnimationChannelPath.scale:
						propertyNames = new string[] { "localScale.x", "localScale.y", "localScale.z" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var scale = data.AsFloat3s[frame].ToUnityVector3Raw();
											  return new float[] { scale.x, scale.y, scale.z };
										  });
						break;

					case GLTFAnimationChannelPath.weights:
						var mesh = targetNode.Mesh.Value;
						var primitives = mesh.Primitives;
						if (primitives[0].Targets == null) break;
						var targetCount = primitives[0].Targets.Count;
						for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
						{
							// see SceneImporter:156
							// blend shapes are always called "Morphtarget"
							var targetNames = mesh.TargetNames;
							propertyNames = new string[targetCount];
							for (var i = 0; i < targetCount; i++)
								propertyNames[i] = _options.ImportBlendShapeNames ? ("blendShape." + ((targetNames != null && targetNames.Count > i) ? targetNames[i] : ("Morphtarget" + i))) : "blendShape."+i.ToString();
							var frameFloats = new float[targetCount];

							var blendShapeFrameWeight = _options.BlendShapeFrameWeight;
							SetAnimationCurve(clip, relativePath, propertyNames, input, output,
								samplerCache.Interpolation, typeof(SkinnedMeshRenderer),
								(data, frame) =>
								{
									var allValues = data.AsFloats;
									for (var k = 0; k < targetCount; k++)
										frameFloats[k] = allValues[frame * targetCount + k] * blendShapeFrameWeight;

									return frameFloats;
								});
						}
						break;

					default:
						Debug.Log(LogType.Warning, $"Cannot read GLTF animation path (File: {_gltfFileName})");
						break;
				} // switch target type
			} // foreach channel

			// EnsureQuaternionContinuity results in unwanted tangents on the first and last keyframes > custom Solution in SetAnimationCurve
			//clip.EnsureQuaternionContinuity();
			return clip;
		}

#endif

	}
}
