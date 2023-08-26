using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;

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
					Stream = inputBufferCacheData.Stream,
					Offset = inputBufferCacheData.ChunkOffset
				};

				samplers[i].Input = attributeAccessor;
				samplersByType["time"].Add(attributeAccessor);

				// set up output accessors
				BufferCacheData outputBufferCacheData = await GetBufferData(samplerDef.Output.Value.BufferView.Value.Buffer);
				attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Output,
					Stream = outputBufferCacheData.Stream,
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

			for (var ci = 0; ci < channelCount; ++ci)
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
				clip.SetCurve(relativePath, curveType, propertyNames[ci], curve);
			}
		}

		private static void SetTangentMode(Keyframe[] keyframes, int keyframeIndex, InterpolationType interpolation)
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
					throw new NotImplementedException();
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

			foreach (AnimationChannel channel in animation.Channels)
			{
				AnimationSamplerCacheData samplerCache = animationCache.Samplers[channel.Sampler.Id];
				if (channel.Target.Node == null)
				{
					// If a channel doesn't have a target node, then just skip it.
					// This is legal and is present in one of the asset generator models, but means that animation doesn't actually do anything.
					// https://github.com/KhronosGroup/glTF-Asset-Generator/tree/master/Output/Positive/Animation_NodeMisc
					// Model 08
					continue;
				}
				var node = await GetNode(channel.Target.Node.Id, cancellationToken);
				string relativePath = RelativePathFrom(node.transform, root);

				NumericArray input = samplerCache.Input.AccessorContent,
					output = samplerCache.Output.AccessorContent;

				string[] propertyNames;
				var known = Enum.TryParse(channel.Target.Path, out GLTFAnimationChannelPath path);
				if (!known) continue;
				switch (path)
				{
					case GLTFAnimationChannelPath.translation:
						propertyNames = new string[] { "localPosition.x", "localPosition.y", "localPosition.z" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var position = data.AsVec3s[frame].ToUnityVector3Convert();
											  return new float[] { position.x, position.y, position.z };
										  });
						break;

					case GLTFAnimationChannelPath.rotation:
						propertyNames = new string[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var rotation = data.AsVec4s[frame];
											  var quaternion = new GLTF.Math.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W).ToUnityQuaternionConvert();
											  return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
										  });

						break;

					case GLTFAnimationChannelPath.scale:
						propertyNames = new string[] { "localScale.x", "localScale.y", "localScale.z" };

						SetAnimationCurve(clip, relativePath, propertyNames, input, output,
										  samplerCache.Interpolation, typeof(Transform),
										  (data, frame) =>
										  {
											  var scale = data.AsVec3s[frame].ToUnityVector3Raw();
											  return new float[] { scale.x, scale.y, scale.z };
										  });
						break;

					case GLTFAnimationChannelPath.weights:
						var primitives = channel.Target.Node.Value.Mesh.Value.Primitives;
						if (primitives[0].Targets == null) break;
						var targetCount = primitives[0].Targets.Count;
						for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
						{
							// see SceneImporter:156
							// blend shapes are always called "Morphtarget" and always have frame weight 100 on import

							var prim = primitives[primitiveIndex];
							var targetNames = prim.TargetNames;
							propertyNames = new string[targetCount];
							for (var i = 0; i < targetCount; i++)
								propertyNames[i] = "blendShape." + (targetNames != null ? targetNames[i] : ("Morphtarget" + i));

							var frameFloats = new float[targetCount];

							SetAnimationCurve(clip, relativePath, propertyNames, input, output,
								samplerCache.Interpolation, typeof(SkinnedMeshRenderer),
								(data, frame) =>
								{
									var allValues = data.AsFloats;
									for (var k = 0; k < targetCount; k++)
										frameFloats[k] = allValues[frame * targetCount + k];

									return frameFloats;
								});
						}
						break;

					default:
						Debug.Log(LogType.Warning, "Cannot read GLTF animation path");
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
