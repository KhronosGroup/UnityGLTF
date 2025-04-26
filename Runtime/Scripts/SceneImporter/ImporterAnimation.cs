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
				BufferCacheData inputBufferCacheData = await GetBufferData(samplerDef.Input.Value.BufferView?.Value?.Buffer);
				AttributeAccessor attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Input,
					bufferData = inputBufferCacheData?.bufferData ?? default,
					Offset = inputBufferCacheData?.ChunkOffset ?? 0,
				};

				samplers[i].Input = attributeAccessor;
				samplersByType["time"].Add(attributeAccessor);

				// set up output accessors
				BufferCacheData outputBufferCacheData = await GetBufferData(samplerDef.Output.Value.BufferView?.Value?.Buffer);
				attributeAccessor = new AttributeAccessor
				{
					AccessorId = samplerDef.Output,
					bufferData = outputBufferCacheData?.bufferData ?? default,
					Offset = outputBufferCacheData?.ChunkOffset ?? 0,
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
					key.inTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex, ref AnyAnimationTimeNotIncreasing);
					key.outTangent = GetCurveKeyframeLeftLinearSlope(keyframes, keyframeIndex + 1, ref AnyAnimationTimeNotIncreasing);
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

		private static float GetCurveKeyframeLeftLinearSlope(Keyframe[] keyframes, int keyframeIndex, ref bool anyNonCreasing)
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
				anyNonCreasing = true;
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

			int[] nodeIds = new int[0];
			
			AnimationPointerImportContext pointerImportContext = null;

			AttributeAccessor FindSecondaryChannel(string animationPointerPath)
			{
				foreach (AnimationChannel secondAnimationChannel in animation.Channels)
				{
					if (secondAnimationChannel.Target.Extensions == null ||
					    !secondAnimationChannel.Target.Extensions.TryGetValue(KHR_animation_pointer.EXTENSION_NAME,
						    out IExtension secondaryExt))
						continue;
					if (secondaryExt is KHR_animation_pointer secondaryPointer)
					{
						AnimationSamplerCacheData secondarySamplerCache =
							animationCache.Samplers[secondAnimationChannel.Sampler.Id];
						if (secondaryPointer.path == animationPointerPath)
						{
							return secondarySamplerCache.Output;
						}
					}
				}

				return null;
			}

			foreach (AnimationChannel channel in animation.Channels)
			{
				bool usesPointer = false;
				IExtension pointerExtension = null;
				AnimationSamplerCacheData samplerCache = animationCache.Samplers[channel.Sampler.Id];
				if (channel.Target.Extensions != null && channel.Target.Extensions.TryGetValue(
					    KHR_animation_pointer.EXTENSION_NAME,
					    out pointerExtension))
				{
					if (Context.TryGetPlugin(out pointerImportContext))
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
					var pointerHierarchy = new PointerPath(relativePath);
					if (!pointerHierarchy.isValid)
						continue;
					
					switch (pointerHierarchy.PathElementType)
					{
						case PointerPath.PathElement.RootExtension:
							if (!_gltfRoot.Extensions.TryGetValue(pointerHierarchy.elementName, out IExtension hierarchyExtension))
								continue;
							
							// Check if the extension support animation pointers
							if (hierarchyExtension is IImportAnimationPointerRootExtension rootExtension)
							{
								// Let the extension handle the pointer data and create the nodeIds and unity properties
								if (rootExtension.TryGetImportAnimationPointerData(_gltfRoot, pointerHierarchy, out pointerData))
									nodeIds = pointerData.targetNodeIds;
								else
									continue;
							}
							pointerData.primaryData = samplerCache.Output; 
							break;
						case PointerPath.PathElement.Root:
							var rootType = pointerHierarchy.elementName;
							var rootIndex = pointerHierarchy.FindNext(PointerPath.PathElement.Index);
							if (rootIndex == null)
								continue;
						
							switch (rootType)
							{
								case "nodes":
									var pointerPropertyElement = pointerHierarchy.FindNext(PointerPath.PathElement.Property);
									if (pointerPropertyElement == null)
										continue;
								
									pointerData = new AnimationPointerData();
									pointerData.targetNodeIds = new int[] {rootIndex.index};
									nodeIds = pointerData.targetNodeIds;

									// Convert translate, scale, rotation from pointer path to to GLTFAnimationChannelPath, so we can use the same code path as the non-animation-pointer channels
									if (!GLTFAnimationChannelPath.TryParse(pointerPropertyElement.elementName, out path))
										continue;

									break; 
								case "materials":
									nodeIds = _gltfRoot.GetAllNodeIdsWithMaterialId(rootIndex.index);
									if (nodeIds.Length == 0)
										continue;
									var materialPath = pointerHierarchy.FindNext(PointerPath.PathElement.Index).next;
									if (materialPath == null)
										continue;
								
									var gltfPropertyPath = materialPath.ExtractPath();
									var mat = _assetCache.MaterialCache[rootIndex.index];
									if (!mat.UnityMaterial)
										continue;
									
									if (!AnimationPointerHelpers.BuildImportMaterialAnimationPointerData(pointer.path, pointerImportContext.materialPropertiesRemapper, mat.UnityMaterial, gltfPropertyPath, samplerCache.Output, out pointerData))
										continue;
									pointerData.targetNodeIds = nodeIds;
									if (string.IsNullOrEmpty(pointerData.primaryPath))
									{
										pointerData.primaryPath = "/" + pointerHierarchy.ExtractPath().Replace(rootIndex.next.ExtractPath(), pointerData.primaryProperty);
										pointerData.primaryData = FindSecondaryChannel(pointerData.primaryPath);
										if (pointerData.primaryData != null)
										{
											//cancel here and process this combined property later when we found first the Primary Property
											continue;
										}
									}
									else
									if (!string.IsNullOrEmpty(pointerData.secondaryProperty))
									{
										// When an property has potentially a second Sampler, we need to find it. e.g. like EmissionFactor and EmissionStrength

										pointerData.secondaryPath = "/" + pointerHierarchy.ExtractPath().Replace(rootIndex.next.ExtractPath(), pointerData.secondaryProperty);
										pointerData.secondaryData = FindSecondaryChannel(pointerData.secondaryPath);
									}
									break;
								case "cameras":
									int cameraId = rootIndex.index;
									pointerData = new AnimationPointerData();
									pointerData.targetType = typeof(Camera);
									pointerData.primaryData = samplerCache.Output; 
	
									string gltfCameraPropertyPath = rootIndex.next.ExtractPath();
									switch (gltfCameraPropertyPath)
									{
										case "orthographic/ymag":
											pointerData.secondaryPath = $"/{pointerHierarchy.elementName}/{rootIndex.index.ToString()}/orthographic/xmag";
											pointerData.unityPropertyNames = new string[] { "orthographic size" };
											pointerData.secondaryData = FindSecondaryChannel(pointerData.secondaryPath);
											pointerData.importAccessorContentConversion = (data, frame) =>
											{
												var xmag = data.secondaryData.AccessorContent.AsFloats[frame];
												var ymag = data.primaryData.AccessorContent.AsFloats[frame];
												return new float[] {Mathf.Max(xmag, ymag)};
											};
											break;
										case "orthographic/xmag":
											continue;
										case "perspective/znear":
										case "orthographic/znear":
											pointerData.unityPropertyNames = new string[] { "near clip plane" };
											pointerData.importAccessorContentConversion = (data, frame) =>
												new float[] {data.primaryData.AccessorContent.AsFloats[frame]};
											break;
										case "perspective/zfar":
										case "orthographic/zfar":
											pointerData.unityPropertyNames = new string[] { "far clip plane" };
											pointerData.importAccessorContentConversion = (data, frame) =>
												new float[] {data.primaryData.AccessorContent.AsFloats[frame]};
											break;
										case "perspective/yfov":
											pointerData.unityPropertyNames = new string[] { "field of view" };
											pointerData.importAccessorContentConversion = (data, frame) =>
											{
												var fov = data.primaryData.AccessorContent.AsFloats[frame] * Mathf.Rad2Deg;
												return new float[] {fov};
											};
											break;
										case "backgroundColor":
											pointerData.unityPropertyNames = new string[] { "background color.r", "background color.g", "background color.b", "background color.a" };
											pointerData.importAccessorContentConversion = (data, frame) =>
											{
												var color = data.primaryData.AccessorContent.AsFloat4s[frame].ToUnityColorRaw();
												return new float[] {color.r, color.g, color.b, color.a};
											};
											break;
										default:
											Debug.Log(LogType.Warning, "Unknown property name on Camera " + cameraId.ToString() + ": " + gltfCameraPropertyPath);
											break;
									}
									
									nodeIds = _gltfRoot.GetAllNodeIdsWithCameraId(cameraId);
									break;
								default:
									continue;
								//throw new NotImplementedException();
							}
							break;
						default:
							continue;
					}
					
					if (pointerData == null)
						continue;
				}
				else
				{
					if (channel.Target == null || channel.Target.Node == null)
						continue;
					nodeIds = new int[] {channel.Target.Node.Id};
				}

				// In case an animated material are referenced from many nodes, whe need to create a curve for each node. (e.g. Materials)
				foreach (var nodeId in nodeIds)
				{
					if (samplerCache.Input == null || samplerCache.Output == null)
						continue;
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
					else
					{
						if (pointerData.targetType == null)
							pointerData.targetType = targetNode.Skin != null
								? typeof(SkinnedMeshRenderer)
								: typeof(MeshRenderer);
					}

					switch (path)
					{
						case GLTFAnimationChannelPath.pointer:
							if (pointerData.importAccessorContentConversion == null)
								continue;
							SetAnimationCurve(clip, relativePath, pointerData.unityPropertyNames, input, output,
								samplerCache.Interpolation, pointerData.targetType,
								(data, frame) => pointerData.importAccessorContentConversion(pointerData, frame));
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
									return new float[]
										{ position.x * factor, position.y * factor, position.z * factor };
#else
											  return new float[] { position.x, position.y, position.z };
#endif
								});
							break;

						case GLTFAnimationChannelPath.rotation:
							propertyNames = new string[]
								{ "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
							bool flipRotation = (targetNode.Extensions != null
							                 && targetNode.Extensions.ContainsKey(KHR_lights_punctualExtensionFactory.EXTENSION_NAME)
							                 && Context.TryGetPlugin<LightsPunctualImportContext>(out _));
							SetAnimationCurve(clip, relativePath, propertyNames, input, output,
								samplerCache.Interpolation, typeof(Transform),
								(data, frame) =>
								{
									var rotation = data.AsFloat4s[frame];
									var quaternion = rotation.ToUnityQuaternionConvert();
									if (flipRotation)
										quaternion *= Quaternion.Euler(0, 180, 0);
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
									propertyNames[i] = _options.ImportBlendShapeNames
										? ("blendShape." + ((targetNames != null && targetNames.Count > i)
											? targetNames[i]
											: ("Morphtarget" + i)))
										: "blendShape." + i.ToString();
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
				} // foreach nodeIds
				
				await YieldOnTimeoutAndThrowOnLowMemory();
			} // foreach channel

			// EnsureQuaternionContinuity results in unwanted tangents on the first and last keyframes > custom Solution in SetAnimationCurve
			//clip.EnsureQuaternionContinuity();
			return clip;
		}

#endif

	}
}
