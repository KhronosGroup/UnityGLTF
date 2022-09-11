#if UNITY_EDITOR
#define ANIMATION_EXPORT_SUPPORTED
#endif

#if ANIMATION_EXPORT_SUPPORTED && (UNITY_ANIMATION || !UNITY_2019_1_OR_NEWER)
#define ANIMATION_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Playables;
using UnityGLTF.Extensions;
using Object = UnityEngine.Object;

#if ANIMATION_EXPORT_SUPPORTED
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
#if ANIMATION_SUPPORTED
		private readonly Dictionary<(AnimationClip clip, float speed), GLTFAnimation> _clipToAnimation = new Dictionary<(AnimationClip, float), GLTFAnimation>();
#endif
#if ANIMATION_SUPPORTED
		private readonly Dictionary<(AnimationClip clip, float speed, string targetPath), Transform> _clipAndSpeedAndPathToExportedTransform = new Dictionary<(AnimationClip, float, string), Transform>();
#endif

#if ANIMATION_SUPPORTED
		private static int AnimationBakingFramerate = 30; // FPS
		private static bool BakeAnimationData = true;
#endif

		// Parses Animation/Animator component and generate a glTF animation for the active clip
		// This may need additional work to fully support animatorControllers
		public void ExportAnimationFromNode(ref Transform transform)
		{
			exportAnimationFromNodeMarker.Begin();

#if ANIMATION_SUPPORTED
			Animator animator = transform.GetComponent<Animator>();
			if (animator)
			{
#if ANIMATION_EXPORT_SUPPORTED
                AnimationClip[] clips = AnimationUtility.GetAnimationClips(transform.gameObject);
                var animatorController = animator.runtimeAnimatorController as AnimatorController;
				// Debug.Log("animator: " + animator + "=> " + animatorController);
                ExportAnimationClips(transform, clips, animator, animatorController);
#endif
			}

			UnityEngine.Animation animation = transform.GetComponent<UnityEngine.Animation>();
			if (animation)
			{
#if ANIMATION_EXPORT_SUPPORTED
                AnimationClip[] clips = UnityEditor.AnimationUtility.GetAnimationClips(transform.gameObject);
                ExportAnimationClips(transform, clips);
#endif
			}
#endif
			exportAnimationFromNodeMarker.End();
		}

#if ANIMATION_SUPPORTED
		private IEnumerable<AnimatorState> GetAnimatorStateParametersForClip(AnimationClip clip, AnimatorController animatorController)
		{
			if (!clip)
				yield break;

			if (!animatorController)
				yield return new AnimatorState() { name = clip.name, speed = 1f };

			foreach (var layer in animatorController.layers)
			{
				foreach (var state in layer.stateMachine.states)
				{
					// find a matching clip in the animator
					if (state.state.motion is AnimationClip c && c == clip)
					{
						yield return state.state;
					}
				}
			}
		}

		private GLTFAnimation GetOrCreateAnimation(AnimationClip clip, string searchForDuplicateName, float speed)
		{
			var existingAnim = default(GLTFAnimation);
			if (_exportOptions.MergeClipsWithMatchingNames)
			{
				// Check if we already exported an animation with exactly that name. If yes, we want to append to the previous one instead of making a new one.
				// This allows to merge multiple animations into one if required (e.g. a character and an instrument that should play at the same time but have individual clips).
				existingAnim = _root.Animations?.FirstOrDefault(x => x.Name == searchForDuplicateName);
			}

			// TODO when multiple AnimationClips are exported, we're currently not properly merging those;
			// we should only export the GLTFAnimation once but then apply that to all nodes that require it (duplicating the animation but not the accessors)
			// instead of naively writing over the GLTFAnimation with the same data.
			var animationClipAndSpeed = (clip, speed);
			if (existingAnim == null)
			{
				if(_clipToAnimation.TryGetValue(animationClipAndSpeed, out existingAnim))
				{
					// we duplicate the clip it was exported before so we can retarget to another transform.
					existingAnim = new GLTFAnimation(existingAnim, _root);
				}
			}

			GLTFAnimation anim = existingAnim != null ? existingAnim : new GLTFAnimation();

			// add to set of already exported clip-state pairs
			if (!_clipToAnimation.ContainsKey(animationClipAndSpeed))
				_clipToAnimation.Add(animationClipAndSpeed, anim);

			return anim;
		}

		// Creates GLTFAnimation for each clip and adds it to the _root
		public void ExportAnimationClips(Transform nodeTransform, IList<AnimationClip> clips, Animator animator = null, AnimatorController animatorController = null)
		{
			// Debug.Log("exporting clips from " + nodeTransform + " with " + animatorController);
			if (animatorController)
			{
				if (!animator) throw new ArgumentNullException("Missing " + nameof(animator));
				for (int i = 0; i < clips.Count; i++)
				{
					if (!clips[i]) continue;

					// special case: there could be multiple states with the same animation clip.
					// if we want to handle this here, we need to find all states that match this clip
					foreach(var state in GetAnimatorStateParametersForClip(clips[i], animatorController))
					{
						var speed = state.speed * (state.speedParameterActive ? animator.GetFloat(state.speedParameter) : 1f);
						var name = clips[i].name;
						ExportAnimationClip(clips[i], name, nodeTransform, speed);
					}
				}
			}
			else
			{
				for (int i = 0; i < clips.Count; i++)
				{
					if (!clips[i]) continue;
					var speed = 1f;
					ExportAnimationClip(clips[i], clips[i].name, nodeTransform, speed);
				}
			}
		}

		public GLTFAnimation ExportAnimationClip(AnimationClip clip, string name, Transform node, float speed)
		{
			if (!clip) return null;
			GLTFAnimation anim = GetOrCreateAnimation(clip, name, speed);

			anim.Name = name;
			if(settings.UniqueAnimationNames)
				anim.Name = ObjectNames.GetUniqueName(_root.Animations.Select(x => x.Name).ToArray(), anim.Name);

			ConvertClipToGLTFAnimation(clip, node, anim, speed);

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0 && !_root.Animations.Contains(anim))
			{
				_root.Animations.Add(anim);
				_animationClips.Add((node, clip));
			}
			return anim;
		}
#endif

#if ANIMATION_SUPPORTED
		public enum AnimationKeyRotationType
		{
			Unknown,
			Quaternion,
			Euler
		}

		public class PropertyCurve
		{
			public string propertyName;
			public Type propertyType;
			public List<AnimationCurve> curve;
			public Object target;

			public PropertyCurve(Object target, EditorCurveBinding binding)
			{
				this.target = target;
				this.propertyName = binding.propertyName;
				curve = new List<AnimationCurve>();
			}

			public float Evaluate(float time, int index)
			{
				if (index < 0 || index >= curve.Count)
				{
					// common case: A not animated but RGB is.
					// TODO this should actually use the value from the material.
					if (propertyType == typeof(Color) && index == 3)
						return 1;

					throw new ArgumentOutOfRangeException(nameof(index), $"PropertyCurve {propertyName} ({propertyType}) has only {curve.Count} curves but index {index} was accessed for time {time}");
				}

				return curve[index].Evaluate(time);
			}
		}

		internal struct TargetCurveSet
		{
			#pragma warning disable 0649
			public AnimationCurve[] translationCurves;
			public AnimationCurve[] rotationCurves;
			public AnimationCurve[] scaleCurves;
			public AnimationKeyRotationType rotationType;
			public Dictionary<string, AnimationCurve> weightCurves;
			public PropertyCurve propertyCurve;
			#pragma warning restore

			public Dictionary<string, PropertyCurve> propertyCurves;

			// for KHR_animation_pointer
			public void AddPropertyCurves(Object animatedObject, AnimationCurve curve, EditorCurveBinding binding)
			{
				if (propertyCurves == null) propertyCurves = new Dictionary<string, PropertyCurve>();
				if (!binding.propertyName.Contains("."))
				{
					var prop = new PropertyCurve(animatedObject, binding);
					prop.curve.Add(curve);
					if (animatedObject is GameObject || animatedObject is Component)
						TryFindMemberBinding(binding, prop, prop.propertyName);
					propertyCurves.Add(binding.propertyName, prop);
				}
				else
				{
					var memberName = binding.propertyName;

					// Color is animated as a Color/Vector4
					if (memberName.EndsWith(".r", StringComparison.Ordinal) || memberName.EndsWith(".g", StringComparison.Ordinal) ||
					    memberName.EndsWith(".b", StringComparison.Ordinal) || memberName.EndsWith(".a", StringComparison.Ordinal) ||
					    memberName.EndsWith(".x", StringComparison.Ordinal) || memberName.EndsWith(".y", StringComparison.Ordinal) ||
					    memberName.EndsWith(".z", StringComparison.Ordinal) || memberName.EndsWith(".w", StringComparison.Ordinal))
						memberName = binding.propertyName.Substring(0, binding.propertyName.LastIndexOf(".", StringComparison.Ordinal));

					if (propertyCurves.TryGetValue(memberName, out var existing))
					{
						existing.curve.Add(curve);
					}
					else
					{
						var prop = new PropertyCurve(animatedObject, binding);
						prop.propertyName = memberName;
						prop.curve.Add(curve);
						propertyCurves.Add(memberName, prop);
						if (memberName.StartsWith("material.") && animatedObject is Renderer rend)
						{
							var mat = rend.sharedMaterial;
							if (!mat)
							{
								Debug.LogWarning("Animated missing material?", animatedObject);
							}
							memberName = memberName.Substring("material.".Length);
							prop.propertyName = memberName;
							prop.target = mat;
							if (memberName.EndsWith("_ST", StringComparison.Ordinal))
							{
								prop.propertyType = typeof(Vector4);
							}
							else
							{
								var found = false;
								for (var i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); i++)
								{
									if (found) break;
									var name = ShaderUtil.GetPropertyName(mat.shader, i);
									if (!memberName.EndsWith(name)) continue;
									found = true;
									var materialProperty = ShaderUtil.GetPropertyType(mat.shader, i);
									switch (materialProperty)
									{
										case ShaderUtil.ShaderPropertyType.Color:
											prop.propertyType = typeof(Color);
											break;
										case ShaderUtil.ShaderPropertyType.Vector:
											prop.propertyType = typeof(Vector4);
											break;
										case ShaderUtil.ShaderPropertyType.Float:
											prop.propertyType = typeof(float);
											break;
										case ShaderUtil.ShaderPropertyType.TexEnv:
											prop.propertyType = typeof(Texture);
											break;
									}
								}
							}
						}
						else if (animatedObject is Light)
						{
							switch (memberName)
							{
								case "m_Color":
									prop.propertyType = typeof(Color);
									break;
								case "m_Intensity":
									prop.propertyType = typeof(float);
									break;
								case "m_SpotAngle":
								case "m_InnerSpotAngle":
									prop.propertyType = typeof(float);
									break;
							}
						}
						else if (animatedObject is Camera)
						{
							// types match, names are explicitly handled in ExporterAnimationPointer.cs
						}
						else
						{
							TryFindMemberBinding(binding, prop, prop.propertyName);
							// var member = FindMemberOnTypeIncludingBaseTypes(binding.type, prop.propertyName);
							// if (member is FieldInfo field) prop.propertyType = field.FieldType;
							// else if (member is PropertyInfo p) prop.propertyType = p.PropertyType;
							// if(prop.propertyType == null)
							// 	Debug.LogWarning($"Member {prop.propertyName} not found on {binding.type}: implicitly handling animated property {prop.propertyName} ({prop.propertyType}) on target {prop.target}", prop.target);
						}
					}
				}
			}

			private static bool TryFindMemberBinding(EditorCurveBinding binding, PropertyCurve prop, string memberName, int iteration = 0)
			{
				// explicitly handled
				if(binding.type == typeof(Camera) || binding.type == typeof(Light))
					return true;

				if (memberName == "m_IsActive")
				{
					prop.propertyType = typeof(float);
					prop.propertyName = "activeSelf";
					return true;
				}

				var member = FindMemberOnTypeIncludingBaseTypes(binding.type, memberName);
				if (member is FieldInfo field) prop.propertyType = field.FieldType;
				else if (member is PropertyInfo p) prop.propertyType = p.PropertyType;
				if (member != null)
				{
					prop.propertyName = member.Name;
					return true;
				}

				if (iteration == 0)
				{
					// some members start with m_, for example m_AnchoredPosition in RectTransform but the field/property name is actually anchoredPosition
					if (memberName.StartsWith("m_"))
					{
						memberName = char.ToLowerInvariant(memberName[2]) + memberName.Substring(3);
						return TryFindMemberBinding(binding, prop, memberName, ++iteration);
					}
				}

				Debug.LogWarning($"Member {prop.propertyName} not found on {binding.type}: implicitly handling animated property {prop.propertyName} ({prop.propertyType}) on target {prop.target}", prop.target);
				return false;
			}

			private static MemberInfo FindMemberOnTypeIncludingBaseTypes(Type type, string memberName)
			{
				while (type != null)
				{
					var member = type.GetMember(memberName, BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (member.Length > 0) return member[0];
					type = type.BaseType;
				}
				return null;
			}

			public void Init()
			{
				translationCurves = new AnimationCurve[3];
				rotationCurves = new AnimationCurve[4];
				scaleCurves = new AnimationCurve[3];
				weightCurves = new Dictionary<string, AnimationCurve>();
			}
		}

		private static string LogObject(object obj)
		{
			if (obj == null) return "null";

			if (obj is Component tr)
				return $"{tr.name} (InstanceID: {tr.GetInstanceID()}, Type: {tr.GetType()})";
			if (obj is GameObject go)
				return $"{go.name} (InstanceID: {go.GetInstanceID()})";

			return obj.ToString();
		}

		private void ConvertClipToGLTFAnimation(AnimationClip clip, Transform transform, GLTFAnimation animation, float speed)
		{
			convertClipToGLTFAnimationMarker.Begin();

			// Generate GLTF.Schema.AnimationChannel and GLTF.Schema.AnimationSampler
			// 1 channel per node T/R/S, one sampler per node T/R/S
			// Need to keep a list of nodes to convert to indexes

			// 1. browse clip, collect all curves and create a TargetCurveSet for each target
			Dictionary<string, TargetCurveSet> targetCurvesBinding = new Dictionary<string, TargetCurveSet>();
			CollectClipCurves(transform.gameObject, clip, targetCurvesBinding);

			// Baking needs all properties, fill missing curves with transform data in 2 keyframes (start, endTime)
			// where endTime is clip duration
			// Note: we should avoid creating curves for a property if none of it's components is animated

			GenerateMissingCurves(clip.length, transform, ref targetCurvesBinding);

			if (BakeAnimationData)
			{
				// Bake animation for all animated nodes
				foreach (string target in targetCurvesBinding.Keys)
				{
					var hadAlreadyExportedThisBindingBefore = _clipAndSpeedAndPathToExportedTransform.TryGetValue((clip, speed, target), out var alreadyExportedTransform);
					Transform targetTr = target.Length > 0 ? transform.Find(target) : transform;
					int newTargetId = targetTr ? GetTransformIndex(targetTr) : -1;

					var targetTrShouldNotBeExported = targetTr && !targetTr.gameObject.activeInHierarchy && !settings.ExportDisabledGameObjects;

					if (hadAlreadyExportedThisBindingBefore && newTargetId < 0)
					{
						// warn: the transform for this binding exists, but its Node isn't exported. It's probably disabled and "Export Disabled" is off.
						if (targetTr)
						{
							Debug.LogWarning("An animated transform is not part of _exportedTransforms, is the object disabled? " + LogObject(targetTr), targetTr);
						}

						// we need to remove the channels and samplers from the existing animation that was passed in if they exist
						int alreadyExportedChannelTargetId = GetTransformIndex(alreadyExportedTransform);
						animation.Channels.RemoveAll(x => x.Target.Node != null && x.Target.Node.Id == alreadyExportedChannelTargetId);

						if (settings.UseAnimationPointer)
						{
							animation.Channels.RemoveAll(x =>
							{
								if (x.Target.Extensions != null && x.Target.Extensions.TryGetValue(KHR_animation_pointer.EXTENSION_NAME, out var ext) && ext is KHR_animation_pointer animationPointer)
								{
									var obj = animationPointer.animatedObject;
									if (obj is Component c)
										obj = c.transform;
									if (obj is Transform tr2 && tr2 == alreadyExportedTransform)
										return true;
								}
								return false;
							});
						}

						// TODO remove all samplers from this animation that were targeting the channels that we just removed
						// TODO: this doesn't work because we're punching holes in the sampler order; all channel sampler IDs would need to be adjusted as well.

						continue;
					}

					if (hadAlreadyExportedThisBindingBefore)
					{
						int alreadyExportedChannelTargetId = GetTransformIndex(alreadyExportedTransform);

						for (int i = 0; i < animation.Channels.Count; i++)
						{
							var existingTarget = animation.Channels[i].Target;
							if (existingTarget.Node != null && existingTarget.Node.Id != alreadyExportedChannelTargetId) continue;

							// if we're here it means that an existing AnimationChannel already targets the same node that we're currently targeting.
							// Without KHR_animation_pointer, that just means we reuse the existing data and tell it to target a new node.
							// With KHR_animation_pointer, we need to do the same, and retarget the path to the new node.
							if (existingTarget.Extensions != null && existingTarget.Extensions.TryGetValue(KHR_animation_pointer.EXTENSION_NAME, out var ext) && ext is KHR_animation_pointer animationPointer)
							{
								// Debug.Log($"export? {!targetTrShouldNotBeExported} - {nameof(existingTarget)}: {L(existingTarget)}, {nameof(animationPointer)}: {L(animationPointer.animatedObject)}, {nameof(alreadyExportedTransform)}: {L(alreadyExportedTransform)}, {nameof(targetTr)}: {L(targetTr)}");
								if (animationPointer.animatedObject is Component c && c.transform == alreadyExportedTransform)
								{
										if (targetTrShouldNotBeExported)
										{
											// Debug.LogWarning("Need to remove this", null);
										}
										else
										{
											var targetType = animationPointer.animatedObject.GetType();
											var newTarget = targetTr.GetComponent(targetType);
											if (newTarget)
											{
												animationPointer.animatedObject = newTarget;
												animationPointer.channel = existingTarget;
												animationPointerResolver.Add(animationPointer);
											}
										}
								}
								else if (animationPointer.animatedObject is Material m)
								{
									var renderer = targetTr.GetComponent<MeshRenderer>();
									if (renderer)
									{
										// TODO we don't have a good way right now to solve this if there's multiple materials on this renderer...
										// would probably need to keep the clip path / binding around and check if that uses a specific index and so on
										var newTarget = renderer.sharedMaterial;
										if (newTarget)
										{
											animationPointer.animatedObject = newTarget;
											animationPointer.channel = existingTarget;
											animationPointerResolver.Add(animationPointer);
										}
									}
								}
							}
							else if (targetTr)
							{
								existingTarget.Node = new NodeId()
								{
									Id = newTargetId,
									Root = _root
								};
							}
						}
						continue;
					}

					if (targetTrShouldNotBeExported)
					{
						Debug.Log("Object " + targetTr + " is disabled, not exporting animated curve " + target, targetTr);
						continue;
					}

					// add to cache: this is the first time we're exporting that particular binding.
					_clipAndSpeedAndPathToExportedTransform.Add((clip, speed, target), targetTr);
					var curve = targetCurvesBinding[target];
					var speedMultiplier = Mathf.Clamp(speed, 0.01f, Mathf.Infinity);

					// Initialize data
					// Bake and populate animation data
					float[] times = null;
					Vector3[] positions = null;
					Quaternion[] rotations = null;
					Vector3[] scales = null;
					float[] weights = null;

					// arbitrary properties require the KHR_animation_pointer extension
					bool sampledAnimationData = false;
					if (settings.UseAnimationPointer && curve.propertyCurves != null && curve.propertyCurves.Count > 0)
					{
						var curves = curve.propertyCurves;
						foreach (KeyValuePair<string, PropertyCurve> c in curves)
						{
							var propertyName = c.Key;
							var prop = c.Value;
							if (BakePropertyAnimation(prop, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values))
							{
								AddAnimationData(prop.target, prop.propertyName, animation, times, values);
								sampledAnimationData = true;
							}
						}
					}

					if (BakeCurveSet(curve, clip.length, AnimationBakingFramerate, speedMultiplier, ref times, ref positions, ref rotations, ref scales, ref weights))
					{
						bool haveAnimation = positions != null || rotations != null || scales != null || weights != null;
						if(haveAnimation)
						{
							AddAnimationData(targetTr, animation, times, positions, rotations, scales, weights);
							sampledAnimationData = true;
						}
						continue;
					}

					if(!sampledAnimationData)
						Debug.LogWarning("Warning: empty animation curves for " + target + " in " + clip + " from " + transform, transform);
				}
			}
			else
			{
				Debug.LogError("Only baked animation is supported for now. Skipping animation", null);
			}

			convertClipToGLTFAnimationMarker.End();
		}

		private void CollectClipCurves(GameObject root, AnimationClip clip, Dictionary<string, TargetCurveSet> targetCurves)
		{
#if UNITY_EDITOR

			if (clip.humanMotion)
			{
				CollectClipCurvesForHumanoid(root, clip, targetCurves);
				return;
			}

			foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
			{
				AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);

				var containsPosition = binding.propertyName.Contains("m_LocalPosition");
				var containsScale = binding.propertyName.Contains("m_LocalScale");
				var containsRotation = binding.propertyName.ToLowerInvariant().Contains("localrotation");
				var containsEuler = binding.propertyName.ToLowerInvariant().Contains("localeuler");
				var containsBlendShapeWeight = binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal);
				var containsCompatibleData = containsPosition || containsScale || containsRotation || containsEuler || containsBlendShapeWeight;

				if (!containsCompatibleData && !settings.UseAnimationPointer)
				{
					Debug.LogWarning("No compatible data found in clip binding: " + binding.propertyName, clip);
					continue;
				}

				if (!targetCurves.ContainsKey(binding.path))
				{
					TargetCurveSet curveSet = new TargetCurveSet();
					curveSet.Init();
					targetCurves.Add(binding.path, curveSet);
				}

				TargetCurveSet current = targetCurves[binding.path];

				if (containsPosition)
				{
					if (binding.propertyName.Contains(".x"))
						current.translationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.translationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.translationCurves[2] = curve;
				}
				else if (containsScale)
				{
					if (binding.propertyName.Contains(".x"))
						current.scaleCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.scaleCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.scaleCurves[2] = curve;
				}
				else if (containsRotation)
				{
					current.rotationType = AnimationKeyRotationType.Quaternion;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
					else if (binding.propertyName.Contains(".w"))
						current.rotationCurves[3] = curve;
				}
				// Takes into account 'localEuler', 'localEulerAnglesBaked' and 'localEulerAnglesRaw'
				else if (containsEuler)
				{
					current.rotationType = AnimationKeyRotationType.Euler;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
				}
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (containsBlendShapeWeight)
				{
					var weightName = binding.propertyName.Substring("blendShape.".Length);
					current.weightCurves.Add(weightName, curve);
				}
				else if (settings.UseAnimationPointer)
				{
					var obj = AnimationUtility.GetAnimatedObject(root, binding);
					if (obj)
					{
						current.AddPropertyCurves(obj, curve, binding);
						targetCurves[binding.path] = current;
					}

					continue;
				}

				targetCurves[binding.path] = current;
			}
#endif
		}

		private void GenerateMissingCurves(float endTime, Transform tr, ref Dictionary<string, TargetCurveSet> targetCurvesBinding)
		{
			var keyList = targetCurvesBinding.Keys.ToList();
			foreach (string target in keyList)
			{
				Transform targetTr = target.Length > 0 ? tr.Find(target) : tr;
				if (targetTr == null)
					continue;

				TargetCurveSet current = targetCurvesBinding[target];

				if (current.weightCurves.Count > 0)
				{
					// need to sort and generate the other matching curves as constant curves for all blend shapes
					var renderer = targetTr.GetComponent<SkinnedMeshRenderer>();
					var mesh = renderer.sharedMesh;
					var shapeCount = mesh.blendShapeCount;

					// need to reorder weights: Unity stores the weights alphabetically in the AnimationClip,
					// not in the order of the weights.
					var newWeights = new Dictionary<string, AnimationCurve>();
					for (int i = 0; i < shapeCount; i++)
					{
						var shapeName = mesh.GetBlendShapeName(i);
						var shapeCurve = current.weightCurves.ContainsKey(shapeName) ? current.weightCurves[shapeName] : CreateConstantCurve(renderer.GetBlendShapeWeight(i), endTime);
						newWeights.Add(shapeName, shapeCurve);
					}

					current.weightCurves = newWeights;
				}

				targetCurvesBinding[target] = current;
			}
		}

		private AnimationCurve CreateConstantCurve(float value, float endTime)
		{
			// No translation curves, adding them
			AnimationCurve curve = new AnimationCurve();
			curve.AddKey(0, value);
			curve.AddKey(endTime, value);
			return curve;
		}

		private bool BakePropertyAnimation(PropertyCurve prop, float length, float bakingFramerate, float speedMultiplier, out float[] times, out object[] values)
		{
			times = null;
			values = null;

			var nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			var deltaTime = length / nbSamples;

			times = new float[nbSamples];
			values = new object[nbSamples];

			// Assuming all the curves exist now
			for (int i = 0; i < nbSamples; ++i)
			{
				float t = i * deltaTime;
				times[i] = t / speedMultiplier;
				if (prop.curve.Count == 1)
				{
					values[i] = prop.curve[0].Evaluate(t);
				}
				else
				{
					var type = prop.propertyType;

					if (typeof(Vector2) == type)
					{
						values[i] = new Vector2(prop.Evaluate(t, 0), prop.Evaluate(t, 1));
					}
					else if (typeof(Vector3) == type)
					{
						values[i] = new Vector3(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2));
					}
					else if (typeof(Vector4) == type)
					{
						values[i] = new Vector4(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3));
					}
					else if (typeof(Color) == type)
					{
						// TODO should actually access r,g,b,a separately since any of these can have curves assigned.
						values[i] = new Color(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3));
					}
					else
					{
						Debug.LogWarning("Property is animated but can't be exported - Name: " + prop.propertyName + ", Type: " + prop.propertyType + ". Does its target exist? You can enable KHR_animation_pointer export in the Project Settings to export more animated properties.", null);
						return false;
					}
				}
			}

			return true;
		}

		private bool BakeCurveSet(TargetCurveSet curveSet, float length, float bakingFramerate, float speedMultiplier, ref float[] times, ref Vector3[] positions, ref Quaternion[] rotations, ref Vector3[] scales, ref float[] weights)
		{
			int nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			float deltaTime = length / nbSamples;
			if(nbSamples > 1)
				nbSamples += 1;
			var weightCount = curveSet.weightCurves?.Count ?? 0;

			bool haveTranslationKeys = curveSet.translationCurves != null && curveSet.translationCurves.Length > 0 && curveSet.translationCurves[0] != null;
			bool haveRotationKeys = curveSet.rotationCurves != null && curveSet.rotationCurves.Length > 0 && curveSet.rotationCurves[0] != null;
			bool haveScaleKeys = curveSet.scaleCurves != null && curveSet.scaleCurves.Length > 0 && curveSet.scaleCurves[0] != null;
			bool haveWeightKeys = curveSet.weightCurves != null && curveSet.weightCurves.Count > 0;

			if(haveScaleKeys)
			{
				if(curveSet.scaleCurves.Length < 3)
				{
					Debug.LogError("Have Scale Animation, but not all properties are animated. Ignoring for now", null);
					return false;
				}
				bool anyIsNull = false;
				foreach (var sc in curveSet.scaleCurves)
					anyIsNull |= sc == null;

				if (anyIsNull)
				{
					Debug.LogWarning("A scale curve has at least one null property curve! Ignoring", null);
					haveScaleKeys = false;
				}
			}

			if(haveRotationKeys)
			{
				bool anyIsNull = false;
				int checkRotationKeyCount = curveSet.rotationType == AnimationKeyRotationType.Euler ? 3 : 4;
				for (int i = 0; i < checkRotationKeyCount; i++)
				{
					anyIsNull |= curveSet.rotationCurves.Length - 1 < i || curveSet.rotationCurves[i] == null;
				}

				if (anyIsNull)
				{
					Debug.LogWarning("A rotation curve has at least one null property curve! Ignoring", null);
					haveRotationKeys = false;
				}
			}

			if(!haveTranslationKeys && !haveRotationKeys && !haveScaleKeys && !haveWeightKeys)
			{
				return false;
			}

			// Initialize Arrays
			times = new float[nbSamples];
			if(haveTranslationKeys)
				positions = new Vector3[nbSamples];
			if(haveScaleKeys)
				scales = new Vector3[nbSamples];
			if(haveRotationKeys)
				rotations = new Quaternion[nbSamples];
			if (haveWeightKeys)
				weights = new float[nbSamples * weightCount];

			// Assuming all the curves exist now
			for (int i = 0; i < nbSamples; ++i)
			{
				float currentTime = i * deltaTime;
				times[i] = currentTime / speedMultiplier;

				if(haveTranslationKeys)
					positions[i] = new Vector3(curveSet.translationCurves[0].Evaluate(currentTime), curveSet.translationCurves[1].Evaluate(currentTime), curveSet.translationCurves[2].Evaluate(currentTime));

				if(haveScaleKeys)
					scales[i] = new Vector3(curveSet.scaleCurves[0].Evaluate(currentTime), curveSet.scaleCurves[1].Evaluate(currentTime), curveSet.scaleCurves[2].Evaluate(currentTime));

				if(haveRotationKeys)
				{
					if (curveSet.rotationType == AnimationKeyRotationType.Euler)
					{
						Quaternion eulerToQuat = Quaternion.Euler(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime));
						rotations[i] = new Quaternion(eulerToQuat.x, eulerToQuat.y, eulerToQuat.z, eulerToQuat.w);
					}
					else
					{
						rotations[i] = new Quaternion(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime), curveSet.rotationCurves[3].Evaluate(currentTime));
					}
				}

				if (haveWeightKeys)
				{
					var curveArray = curveSet.weightCurves.Values.ToArray();
					for(int j = 0; j < weightCount; j++)
					{
						weights[i * weightCount + j] = curveArray[j].Evaluate(times[i]);
					}
				}
			}

			RemoveUnneededKeyframes(ref times, ref positions, ref rotations, ref scales, ref weights, ref weightCount);

			return true;
		}

#endif

		[Obsolete("Please use " + nameof(GetTransformIndex), false)]
		public int GetNodeIdFromTransform(Transform transform)
		{
			return GetTransformIndex(transform);
		}

		internal int GetIndex(object obj)
		{
			switch (obj)
			{
				case Material m: return GetMaterialIndex(m);
				case Light l: return GetLightIndex(l);
				case Camera c: return GetCameraIndex(c);
				case Transform t: return GetTransformIndex(t);
				case GameObject g: return GetTransformIndex(g.transform);
				case Component k: return GetTransformIndex(k.transform);
			}

			return -1;
		}

		public int GetTransformIndex(Transform transform)
		{
			if (transform && _exportedTransforms.TryGetValue(transform.GetInstanceID(), out var index)) return index;
			return -1;
		}

		public int GetMaterialIndex(Material mat)
		{
			if (mat && _exportedMaterials.TryGetValue(mat.GetInstanceID(), out var index)) return index;
			return -1;
		}

		public int GetLightIndex(Light light)
		{
			if (light && _exportedLights.TryGetValue(light.GetInstanceID(), out var index)) return index;
			return -1;
		}

		public int GetCameraIndex(Camera cam)
		{
			if (cam && _exportedCameras.TryGetValue(cam.GetInstanceID(), out var index)) return index;
			return -1;
		}

		public IEnumerable<(int subMeshIndex, MeshPrimitive prim)> GetPrimitivesForMesh(Mesh mesh)
		{
			if (!_meshToPrims.TryGetValue(mesh, out var prims)) yield break;
			foreach (var k in prims.subMeshPrimitives)
			{
				yield return (k.Key, k.Value);
			}
		}

		public void AddAnimationData(
			Transform target,
			GLTF.Schema.GLTFAnimation animation,
			float[] times = null,
			Vector3[] positions = null,
			Quaternion[] rotations = null,
			Vector3[] scales = null,
			float[] weights = null)
		{
			if (!target)
			{
				UnityEngine.Debug.LogWarning("Can not add animation data: missing target transform. " +  animation?.Name);
				return;
			}

			addAnimationDataMarker.Begin();

			int channelTargetId = GetTransformIndex(target);
			if (channelTargetId < 0)
			{
				Debug.LogWarning($"An animated transform seems to be {(settings.ExportDisabledGameObjects ? "missing" : "missing or disabled")}: {target.name} (InstanceID: {target.GetInstanceID()})", target);
				addAnimationDataMarker.End();
				return;
			}

			var animatedNode = _root.Nodes[channelTargetId];
			var needsFlippedLookDirection = animatedNode.Light != null || animatedNode.Camera != null;

			AccessorId timeAccessor = ExportAccessor(times);
			timeAccessor.Value.BufferView.Value.ByteStride = 0;

			// Translation
			if(positions != null && positions.Length > 0)
			{
				exportPositionAnimationDataMarker.Begin();

				AnimationChannel Tchannel = new AnimationChannel();
				AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
				TchannelTarget.Path = GLTFAnimationChannelPath.translation.ToString();
				TchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Tchannel.Target = TchannelTarget;

				AnimationSampler Tsampler = new AnimationSampler();
				Tsampler.Input = timeAccessor;
				Tsampler.Output = ExportAccessor(SchemaExtensions.ConvertVector3CoordinateSpaceAndCopy(positions, SchemaExtensions.CoordinateSpaceConversionScale));
				Tsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Tchannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Tsampler);
				animation.Channels.Add(Tchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "translation", TchannelTarget);

				exportPositionAnimationDataMarker.End();
			}

			// Rotation
			if(rotations != null && rotations.Length > 0)
			{
				exportRotationAnimationDataMarker.Begin();

				AnimationChannel Rchannel = new AnimationChannel();
				AnimationChannelTarget RchannelTarget = new AnimationChannelTarget();
				RchannelTarget.Path = GLTFAnimationChannelPath.rotation.ToString();
				RchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Rchannel.Target = RchannelTarget;

				AnimationSampler Rsampler = new AnimationSampler();
				Rsampler.Input = timeAccessor; // Float, for time
				Rsampler.Output = ExportAccessorSwitchHandedness(rotations, needsFlippedLookDirection); // Vec4 for rotations
				Rsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Rchannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Rsampler);
				animation.Channels.Add(Rchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "rotation", RchannelTarget);

				exportRotationAnimationDataMarker.End();
			}

			// Scale
			if(scales != null && scales.Length > 0)
			{
				exportScaleAnimationDataMarker.Begin();

				AnimationChannel Schannel = new AnimationChannel();
				AnimationChannelTarget SchannelTarget = new AnimationChannelTarget();
				SchannelTarget.Path = GLTFAnimationChannelPath.scale.ToString();
				SchannelTarget.Node = new NodeId
				{
					Id = channelTargetId,
					Root = _root
				};

				Schannel.Target = SchannelTarget;

				AnimationSampler Ssampler = new AnimationSampler();
				Ssampler.Input = timeAccessor; // Float, for time
				Ssampler.Output = ExportAccessor(scales); // Vec3 for scale
				Ssampler.Output.Value.BufferView.Value.ByteStride = 0;
				Schannel.Sampler = new AnimationSamplerId
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Ssampler);
				animation.Channels.Add(Schannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "scale", SchannelTarget);

				exportScaleAnimationDataMarker.End();
			}

			if (weights != null && weights.Length > 0)
			{
				exportWeightsAnimationDataMarker.Begin();

				// scale weights correctly if there are any
				var skinnedMesh = target.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMesh)
				{
					// this code is adapted from SkinnedMeshRendererEditor (which calculates the right range for sliders to show)
					// instead of calculating per blend shape, we're assuming all blendshapes have the same min/max here though.
					var minBlendShapeFrameWeight = 0.0f;
					var maxBlendShapeFrameWeight = 0.0f;

					var sharedMesh = skinnedMesh.sharedMesh;
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

					// Debug.Log($"min: {minBlendShapeFrameWeight}, max: {maxBlendShapeFrameWeight}");
					// glTF weights 0..1 match to Unity weights 0..100, but Unity weights can be in arbitrary ranges
					if (maxBlendShapeFrameWeight > 0)
					{
						for (int i = 0; i < weights.Length; i++)
							weights[i] *= 1 / maxBlendShapeFrameWeight;
					}
				}

				AnimationChannel Wchannel = new AnimationChannel();
				AnimationChannelTarget WchannelTarget = new AnimationChannelTarget();
				WchannelTarget.Path = GLTFAnimationChannelPath.weights.ToString();
				WchannelTarget.Node = new NodeId()
				{
					Id = channelTargetId,
					Root = _root
				};

				Wchannel.Target = WchannelTarget;

				AnimationSampler Wsampler = new AnimationSampler();
				Wsampler.Input = timeAccessor;
				Wsampler.Output = ExportAccessor(weights);
				Wsampler.Output.Value.BufferView.Value.ByteStride = 0;
				Wchannel.Sampler = new AnimationSamplerId()
				{
					Id = animation.Samplers.Count,
					GLTFAnimation = animation,
					Root = _root
				};

				animation.Samplers.Add(Wsampler);
				animation.Channels.Add(Wchannel);

				if (settings.UseAnimationPointer)
					ConvertToAnimationPointer(target, "weights", WchannelTarget);

				exportWeightsAnimationDataMarker.End();
			}

			addAnimationDataMarker.End();
		}

		private static void DecomposeEmissionColor(Color input, out Color output, out float intensity)
		{
			var emissiveAmount = input.linear;
			var maxEmissiveAmount = Mathf.Max(emissiveAmount.r, emissiveAmount.g, emissiveAmount.b);
			if (maxEmissiveAmount > 1)
			{
				emissiveAmount.r /= maxEmissiveAmount;
				emissiveAmount.g /= maxEmissiveAmount;
				emissiveAmount.b /= maxEmissiveAmount;
			}
			emissiveAmount.a = Mathf.Clamp01(emissiveAmount.a);

			// this feels wrong but leads to the right results, probably the above calculations are in the wrong color space
			maxEmissiveAmount = Mathf.LinearToGammaSpace(maxEmissiveAmount);

			output = emissiveAmount;
			intensity = maxEmissiveAmount;
		}

		private static void DecomposeScaleOffset(Vector4 input, out Vector2 scale, out Vector2 offset)
		{
			scale = new Vector2(input.x, input.y);
			offset = new Vector2(input.z, 1 - input.w - input.y);
		}

		private bool ArrayRangeEquals(float[] array, int sectionLength, int lastExportedSectionStart, int prevSectionStart, int sectionStart, int nextSectionStart)
		{
			var equals = true;
			for (int i = 0; i < sectionLength; i++)
			{
				equals &= (lastExportedSectionStart >= prevSectionStart || array[lastExportedSectionStart + i] == array[sectionStart + i]) &&
				          array[prevSectionStart + i] == array[sectionStart + i] &&
				          array[sectionStart + i] == array[nextSectionStart + i];
				if (!equals) return false;
			}

			return true;
		}

		public void RemoveUnneededKeyframes(ref float[] times, ref Vector3[] positions, ref Quaternion[] rotations, ref Vector3[] scales, ref float[] weights, ref int weightCount)
		{
			removeAnimationUnneededKeyframesMarker.Begin();
			removeAnimationUnneededKeyframesInitMarker.Begin();

			var haveTranslationKeys = positions?.Any() ?? false;
			var haveRotationKeys = rotations?.Any() ?? false;
			var haveScaleKeys = scales?.Any() ?? false;
			var haveWeightKeys = weights?.Any() ?? false;

			// remove keys again where prev/next keyframe are identical
			List<float> t2 = new List<float>(times.Length);
			List<Vector3> p2 = new List<Vector3>(times.Length);
			List<Vector3> s2 = new List<Vector3>(times.Length);
			List<Quaternion> r2 = new List<Quaternion>(times.Length);
			List<float> w2 = new List<float>(times.Length);
			var singleFrameWeights = new float[weightCount];

			t2.Add(times[0]);
			if (haveTranslationKeys) p2.Add(positions[0]);
			if (haveRotationKeys) r2.Add(rotations[0]);
			if (haveScaleKeys) s2.Add(scales[0]);
			if (haveWeightKeys)
			{
				Array.Copy(weights, 0, singleFrameWeights, 0, weightCount);
				w2.AddRange(singleFrameWeights);
			}

			removeAnimationUnneededKeyframesInitMarker.End();

			int lastExportedIndex = 0;
			for (int i = 1; i < times.Length - 1; i++)
			{
				removeAnimationUnneededKeyframesCheckIdenticalMarker.Begin();
				// check identical
				bool isIdentical = true;
				if (haveTranslationKeys)
					isIdentical &= (lastExportedIndex >= i - 1 || positions[lastExportedIndex] == positions[i]) && positions[i - 1] == positions[i] && positions[i] == positions[i + 1];
				if (isIdentical && haveRotationKeys)
					isIdentical &= (lastExportedIndex >= i - 1 || rotations[lastExportedIndex] == rotations[i]) && rotations[i - 1] == rotations[i] && rotations[i] == rotations[i + 1];
				if (isIdentical && haveScaleKeys)
					isIdentical &= (lastExportedIndex >= i - 1 || scales[lastExportedIndex] == scales[i]) && scales[i - 1] == scales[i] && scales[i] == scales[i + 1];
				exportWeightsAnimationDataMarker.Begin();
				if (isIdentical && haveWeightKeys)
					isIdentical &= ArrayRangeEquals(weights, weightCount, lastExportedIndex * weightCount, (i - 1) * weightCount, i * weightCount, (i + 1) * weightCount);

				exportWeightsAnimationDataMarker.End();

				if (!isIdentical)
				{
					removeAnimationUnneededKeyframesCheckIdenticalKeepMarker.Begin();
					lastExportedIndex = i;
					t2.Add(times[i]);
					if (haveTranslationKeys) p2.Add(positions[i]);
					if (haveRotationKeys) r2.Add(rotations[i]);
					if (haveScaleKeys) s2.Add(scales[i]);
					exportWeightsAnimationDataMarker.Begin();
					if (haveWeightKeys)
					{
						Array.Copy(weights, (i - 1) * weightCount, singleFrameWeights, 0, weightCount);
						w2.AddRange(singleFrameWeights);
					}
					exportWeightsAnimationDataMarker.End();
					removeAnimationUnneededKeyframesCheckIdenticalKeepMarker.End();
				}
				removeAnimationUnneededKeyframesCheckIdenticalMarker.End();
			}

			removeAnimationUnneededKeyframesFinalizeMarker.Begin();

			var max = times.Length - 1;

			t2.Add(times[max]);
			if (haveTranslationKeys) p2.Add(positions[max]);
			if (haveRotationKeys) r2.Add(rotations[max]);
			if (haveScaleKeys) s2.Add(scales[max]);
			if (haveWeightKeys)
			{
				var skipped = weights.Skip((max - 1) * weightCount).ToArray();
				w2.AddRange(skipped.Take(weightCount));
			}

			// Debug.Log("Keyframes before compression: " + times.Length + "; " + "Keyframes after compression: " + t2.Count);

			times = t2.ToArray();
			if (haveTranslationKeys) positions = p2.ToArray();
			if (haveRotationKeys) rotations = r2.ToArray();
			if (haveScaleKeys) scales = s2.ToArray();
			if (haveWeightKeys) weights = w2.ToArray();

			removeAnimationUnneededKeyframesFinalizeMarker.End();

			removeAnimationUnneededKeyframesMarker.End();
		}
	}
}
