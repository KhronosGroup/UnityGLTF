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
using UnityGLTF.Extensions;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

#if UNITY_2020_2_OR_NEWER
using UnityEngine.Animations;
#endif

#if ANIMATION_EXPORT_SUPPORTED
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
#if ANIMATION_SUPPORTED
		private readonly Dictionary<(AnimationClip clip, float speed, Avatar avatar), GLTFAnimation> _clipToAnimation = new Dictionary<(AnimationClip, float, Avatar), GLTFAnimation>();
		private readonly Dictionary<(AnimationClip clip, float speed, string targetPath), Transform> _clipAndSpeedAndPathToExportedTransform = new Dictionary<(AnimationClip, float, string), Transform>();
		private readonly Dictionary<(AnimationClip clip, float speed, Transform transform), GLTFAnimation> _clipAndSpeedAndNodeToAnimation = new Dictionary<(AnimationClip, float, Transform), GLTFAnimation>();

		private static int AnimationBakingFramerate = 30; // FPS
		private static bool BakeAnimationData = true;
#endif

		private bool? _useAnimationPointer = null;

		private bool UseAnimationPointer
		{
			get
			{
				if (_useAnimationPointer == null)
					_useAnimationPointer = _plugins.Any(x => x is AnimationPointerExportContext);
				return _useAnimationPointer.Value;
			}
		}

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

                // make sure the default state is the first clip
                if (animatorController && animatorController.layers.Length > 0)
                {
	                var defaultState = animatorController.layers[0].stateMachine.defaultState;
	                if (defaultState) {
						var defaultMotion = defaultState.motion;
						if (defaultMotion is AnimationClip clip && clip)
						{
							// make sure this is the first clip in the clips array
							var index = Array.IndexOf(clips, clip);
							if (index > 0)
							{
								var temp = clips[0];
								clips[0] = clip;
								clips[index] = temp;
							}
						}
	                }
                }

				// Debug.Log("animator: " + animator + "=> " + animatorController);
                ExportAnimationClips(transform, clips, animator, animatorController);
#endif
			}

			UnityEngine.Animation animation = transform.GetComponent<UnityEngine.Animation>();
			if (animation)
			{
#if ANIMATION_EXPORT_SUPPORTED
                AnimationClip[] clips = UnityEditor.AnimationUtility.GetAnimationClips(transform.gameObject);

                // make sure the default state is the first clip
                if (animation.clip) {
					var index = Array.IndexOf(clips, animation.clip);
					if (index > 0) {
						var temp = clips[0];
						clips[0] = animation.clip;
						clips[index] = temp;
					}
                }
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

		private GLTFAnimation GetOrCreateAnimation(AnimationClip clip, string searchForDuplicateName, float speed, Transform node)
		{
			var existingAnim = default(GLTFAnimation);
			
			// Check if there's an existing animation for this clip, speed and node -
			// In that case we don't want to duplicate/retarget anything and just return the existing animation.
			// There's also no need to add more data to it since the full state has already been processed.
			// This can happen when e.g. the GetOrCreateAnimation API is called multiple times for the same node and clip –
			// it doesn't happen internally in UnityGLTF
			var animationClipAndSpeedAndNode = (clip, speed, node);
			if (_clipAndSpeedAndNodeToAnimation.TryGetValue(animationClipAndSpeedAndNode, out existingAnim))
			{
				return existingAnim;
			}
			
			if (_exportContext.MergeClipsWithMatchingNames)
			{
				// Check if we already exported an animation with exactly that name. If yes, we want to append to the previous one instead of making a new one.
				// This allows to merge multiple animations into one if required (e.g. a character and an instrument that should play at the same time but have individual clips).
				existingAnim = _root.Animations?.FirstOrDefault(x => x.Name == searchForDuplicateName);
			}

			// TODO when multiple AnimationClips are exported, we're currently not properly merging those;
			// we should only export the GLTFAnimation once but then apply that to all nodes that require it (duplicating the animation but not the accessors)
			// instead of naively writing over the GLTFAnimation with the same data.
			var animator = node.GetComponent<Animator>();
			var avatar = animator ? animator.avatar : null;
			var animationClipAndSpeedAndAvatar = (clip, speed, avatar);
			if (existingAnim == null)
			{
				if(_clipToAnimation.TryGetValue(animationClipAndSpeedAndAvatar, out existingAnim))
				{
					// we duplicate the clip it was exported before so we can retarget to another transform.
					existingAnim = new GLTFAnimation(existingAnim, _root);
				}
			}

			GLTFAnimation anim = existingAnim != null ? existingAnim : new GLTFAnimation();

			// add to set of already exported clip-state pairs
			if (!_clipToAnimation.ContainsKey(animationClipAndSpeedAndAvatar))
				_clipToAnimation.Add(animationClipAndSpeedAndAvatar, anim);
			
			// add to set of already exported clip-node pairs
			if (!_clipAndSpeedAndNodeToAnimation.ContainsKey(animationClipAndSpeedAndNode))
				_clipAndSpeedAndNodeToAnimation.Add(animationClipAndSpeedAndNode, anim);

			return anim;
		}

		// Creates GLTFAnimation for each clip and adds it to the _root
		public void ExportAnimationClips(Transform nodeTransform, IList<AnimationClip> clips, Animator animator = null, AnimatorController animatorController = null)
		{
			// When sampling animation using AnimationMode the animator might be disabled afterwards when the animation is exported from a prefab (e.g. Prefab -> object with humanoid animation -> export from referenced prefab -> animator is disabled)
			// Here we ensure that the animator is enabled again after export
			// See ExportAnimationHumanoid with StartAnimationMode
			var animatorEnabled = !animator || animator.enabled;

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
						var speed = 1f;
						if (settings.BakeAnimationSpeed)
						{
							speed = state.speed * (state.speedParameterActive ? animator.GetFloat(state.speedParameter) : 1f);
						}
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

			if(animator)
				animator.enabled = animatorEnabled;
		}

		public GLTFAnimation ExportAnimationClip(AnimationClip clip, string name, Transform node, float speed)
		{
			if (!clip) return null;
			GLTFAnimation anim = GetOrCreateAnimation(clip, name, speed, node);
			// early out if this is exactly an animation that has already been exported. Not retargeted or changed otherwise.
			if (_root.Animations.Contains(anim)) return anim;

			anim.Name = name;
			if(settings.UniqueAnimationNames)
				anim.Name = ObjectNames.GetUniqueName(_root.Animations.Select(x => x.Name).ToArray(), anim.Name);

			ConvertClipToGLTFAnimation(clip, node, anim, speed);
			
			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0 && !_root.Animations.Contains(anim))
			{
				_root.Animations.Add(anim);
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
			public List<string> curveName;
			public Object target;

			public PropertyCurve(Object target, string propertyName)
			{
				this.target = target;
				this.propertyName = propertyName;
				curve = new List<AnimationCurve>();
				curveName = new List<string>();
			}

			public void AddCurve(AnimationCurve animCurve, string name)
			{
				this.curve.Add(animCurve);
				this.curveName.Add(name);
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

			private static readonly Dictionary<Type, int> requiredCurveCount = new Dictionary<Type, int>()
			{
				{typeof(Color), 4},
				{typeof(Vector2), 2},
				{typeof(Vector3), 3},
				{typeof(Vector4), 4},
			};

			private static readonly string[] colorPropertiesWithoutAlphaChannel = new[]
			{
				"_EmissionColor",
				"_EmissiveFactor",
				"emissiveFactor",
				"_AttenuationColor",
				"attenuationColor",
				"_SpecularColorFactor",
				"specularColorFactor",
				"_SheenColorFactor",
				"sheenColorFactor",
			};

			internal bool Validate()
			{
				if (propertyType == null)
				{
					if (!target)
					{
						// the warning should already been printed during export (at which point we also have more context (like the Object the missing target belongs to so we can ping it)
					}
					else if (target is Material mat)
						Debug.Log(LogType.Warning, (object) $"Animated material property {propertyName} does not exist on material {mat}{(mat ? " / shader " + mat.shader : "")}. Will not be exported", mat);
					else
						Debug.Log(LogType.Warning, (object) $"Curve of animated property has no property type, can not validate {propertyName} on {target}. Will not be exported.", target);
					return false;
				}

				if (requiredCurveCount.TryGetValue(propertyType, out var requiredCount))
				{
					var hasEnoughCurves = curve.Count == requiredCount;

					// Special case for colors, which can have either 3 (rgb) or 4 (rgba) components when animated.
					// When they have 3 components, we also need to check that these are r,g,b - theoretically someone can animate r,g,a and then we get wrong data.
					if (propertyType == typeof(Color) && (
						    (target is Material && colorPropertiesWithoutAlphaChannel.Contains(propertyName)) ||
						    (target is Light && propertyName == "m_Color") ||
						    (target is Camera)))
					{
						requiredCount = 3;
						hasEnoughCurves = curve.Count >= requiredCount
						                  && curveName[0].EndsWith(".r", StringComparison.Ordinal)
						                  && curveName[1].EndsWith(".g", StringComparison.Ordinal)
						                  && curveName[2].EndsWith(".b", StringComparison.Ordinal);
										  // doesn't hurt if there's an extra alpha channel;
										  // seems that happens sometimes when animating, sometimes not

						if (!hasEnoughCurves)
						{
							Debug.Log(LogType.Warning, (object) $"<b>Can not export animation, please animate all three channels (R,G,B) for \"{propertyName}\" on {target}</b>", target);
							return false;
						}
					}

					if (!hasEnoughCurves)
					{
						Debug.Log(LogType.Warning, (object) $"<b>Can not export animation, please animate all channels for \"{propertyName}\"</b>, expected channel count is {requiredCount} but got only {curve.Count}", target);
						return false;
					}
				}

				return true;
			}

			/// <summary>
			/// Call this method once before beginning to evaluate curves
			/// </summary>
			internal void SortCurves()
			{
				// If we animate a color property in Unity and start by creating keys for green then the green curve will be at index 0
				// This method ensures that the curves are in a known order e.g. rgba (instead of green red blue alpha)
				if (curve?.Count > 0 && curveName.Count > 0)
				{
					if (propertyType == typeof(Color))
					{
						FillTempLists();
						var index1 = FindIndex(name => name.EndsWith(".r", StringComparison.Ordinal));
						var index2 = FindIndex(name => name.EndsWith(".g", StringComparison.Ordinal));
						var index3 = FindIndex(name => name.EndsWith(".b", StringComparison.Ordinal));
						var index4 = FindIndex(name => name.EndsWith(".a", StringComparison.Ordinal));
						SortCurves(index1, index2, index3, index4);
					}
					else if (propertyType == typeof(Vector2))
					{
						FillTempLists();
						var index1 = FindIndex(name => name.EndsWith(".x", StringComparison.Ordinal));
						var index2 = FindIndex(name => name.EndsWith(".y", StringComparison.Ordinal));
						SortCurves(index1, index2);
					}
					else if (propertyType == typeof(Vector3))
					{
						FillTempLists();
						var index1 = FindIndex(name => name.EndsWith(".x", StringComparison.Ordinal));
						var index2 = FindIndex(name => name.EndsWith(".y", StringComparison.Ordinal));
						var index3 = FindIndex(name => name.EndsWith(".z", StringComparison.Ordinal));
						SortCurves(index1, index2, index3);
					}
					else if (propertyType == typeof(Vector4))
					{
						FillTempLists();
						var index1 = FindIndex(name => name.EndsWith(".x", StringComparison.Ordinal) || name.EndsWith(".r", StringComparison.Ordinal));
						var index2 = FindIndex(name => name.EndsWith(".y", StringComparison.Ordinal) || name.EndsWith(".g", StringComparison.Ordinal));
						var index3 = FindIndex(name => name.EndsWith(".z", StringComparison.Ordinal) || name.EndsWith(".b", StringComparison.Ordinal));
						var index4 = FindIndex(name => name.EndsWith(".w", StringComparison.Ordinal) || name.EndsWith(".a", StringComparison.Ordinal));
						SortCurves(index1, index2, index3, index4);
					}
				}
			}

			private void SortCurves(int i0, int i1, int i2 = -1, int i3 = -1)
			{
				for(var i = 0; i < curve.Count; i++)
				{
					var curveIndex = i;
					if (i == 0) curveIndex = i0;
					else if (i == 1) curveIndex = i1;
					else if(i == 2 && i2 >= 0) curveIndex = i2;
					else if(i == 3 && i3 >= 0) curveIndex = i3;
					if (curveIndex >= 0 && curveIndex != i)
					{
						this.curve[i] = _tempList1[curveIndex];;
						this.curveName[i] = _tempList2[curveIndex];;
					}
				}
			}

			private static readonly List<AnimationCurve> _tempList1 = new List<AnimationCurve>();
			private static readonly List<string> _tempList2 = new List<string>();

			private void FillTempLists()
			{
				_tempList1.Clear();
				_tempList2.Clear();
				_tempList1.AddRange(curve);
				_tempList2.AddRange(curveName);
			}

			public int FindIndex(Predicate<string> test)
			{
				for(var i = 0; i < curveName.Count; i++)
				{
					if (test(curveName[i]))
						return i;
				}
				return -1;
			}


		}

		internal struct TargetCurveSet
		{
			#pragma warning disable 0649
			public AnimationCurve[] translationCurves;
			public AnimationCurve[] rotationCurves;
			public AnimationCurve[] scaleCurves;
			public Dictionary<string, AnimationCurve> weightCurves;
			public PropertyCurve propertyCurve;
			#pragma warning restore

			public Dictionary<string, PropertyCurve> propertyCurves;

			// for KHR_animation_pointer
			public void AddPropertyCurves(Object animatedObject, AnimationCurve curve, EditorCurveBinding binding)
			{
				// Debug.Log("Adding property curve " + binding.propertyName + " to " + animatedObject);
				if (propertyCurves == null) propertyCurves = new Dictionary<string, PropertyCurve>();
				var memberName = binding.propertyName;
				if (!memberName.Contains("."))
				{
					var prop = new PropertyCurve(animatedObject, memberName);
					prop.AddCurve(curve, memberName);
					if (animatedObject is Light)
					{
						switch (memberName)
						{
							case "m_Color":
								prop.propertyType = typeof(Color);
								break;
							case "m_Intensity":
							case "m_Range":
							case "m_SpotAngle":
							case "m_InnerSpotAngle":
								prop.propertyType = typeof(float);
								break;
						}
					}
					else if (animatedObject is Camera)
					{
						prop.propertyType = typeof(float);
					}
					else if (animatedObject is GameObject || animatedObject is Component)
					{
						TryFindMemberBinding(binding, prop, prop.propertyName);
					}

					if (propertyCurves.ContainsKey(memberName))
					{
						Debug.LogError("Animating the same property on multiple components is currently not supported: " + memberName, animatedObject);
					}
					else
						propertyCurves.Add(memberName, prop);
				}
				else
				{
					// Color is animated as a Color/Vector4
					if (memberName.EndsWith(".r", StringComparison.Ordinal) || memberName.EndsWith(".g", StringComparison.Ordinal) ||
					    memberName.EndsWith(".b", StringComparison.Ordinal) || memberName.EndsWith(".a", StringComparison.Ordinal) ||
					    memberName.EndsWith(".x", StringComparison.Ordinal) || memberName.EndsWith(".y", StringComparison.Ordinal) ||
					    memberName.EndsWith(".z", StringComparison.Ordinal) || memberName.EndsWith(".w", StringComparison.Ordinal))
						memberName = binding.propertyName.Substring(0, binding.propertyName.LastIndexOf(".", StringComparison.Ordinal));

					if (propertyCurves.TryGetValue(memberName, out var existing))
					{
						existing.AddCurve(curve, binding.propertyName);
					}
					else
					{
						var prop = new PropertyCurve(animatedObject, binding.propertyName);
						prop.propertyName = memberName;
						prop.AddCurve(curve, binding.propertyName);
						propertyCurves.Add(memberName, prop);
						if (memberName.StartsWith("material.") && animatedObject is Renderer rend)
						{
							var mat = rend.sharedMaterial;
							if (!mat)
							{
								Debug.LogWarning("Animation Export", $"Animated material is missing {memberName} {mat?.name}", animatedObject);
							}
							memberName = memberName.Substring("material.".Length);
							prop.propertyName = memberName;
							prop.target = mat;
							if (memberName.EndsWith("_ST", StringComparison.Ordinal))
							{
								prop.propertyType = typeof(Vector4);
							}
							else if(mat)
							{
								var found = false;
								var shaderPropertyCount = ShaderUtil.GetPropertyCount(mat.shader);
								// var shaderPropertyNames = Enumerable.Range(0, shaderPropertyCount).Select(x => ShaderUtil.GetPropertyName(mat.shader, x));

								for (var i = 0; i < shaderPropertyCount; i++)
								{
									if (found) break;
									var name = ShaderUtil.GetPropertyName(mat.shader, i);
									if (!memberName.EndsWith(name, StringComparison.Ordinal)) continue;
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
										case ShaderUtil.ShaderPropertyType.Range:
											prop.propertyType = typeof(float);
											break;
										case ShaderUtil.ShaderPropertyType.TexEnv:
											prop.propertyType = typeof(Texture);
											break;
#if UNITY_2021_1_OR_NEWER
										case ShaderUtil.ShaderPropertyType.Int:
											prop.propertyType = typeof(int);
											break;
#endif
										default:
											Debug.LogWarning(null, "Looks like there's a new shader property type - please report a bug!");
											break;
									}
								}
							}
							// The type can still be missing if the animated property doesnt exist on the shader anymore
							if (mat && prop.propertyType == null)
							{
								foreach (var name in prop.curveName)
								{
									if (prop.propertyType != null) break;
									// we can only really resolve a color here by the name
									if (name.EndsWith(".r") || name.EndsWith(".g") || name.EndsWith(".b") || name.EndsWith(".a"))
										prop.propertyType = typeof(Color);
								}
								if (prop.propertyType == null)
									Debug.LogWarning("Animation Export", "Animated property is missing/unknown: " + binding.propertyName, animatedObject);
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
								case "m_Range":
								case "m_SpotAngle":
								case "m_InnerSpotAngle":
									prop.propertyType = typeof(float);
									break;
							}
						}
						else if (animatedObject is Camera)
						{
							// types match, names are explicitly handled in ExporterAnimationPointer.cs
							var lowercaseName = prop.propertyName.ToLowerInvariant();
							if (lowercaseName.Contains("m_backgroundcolor"))
							{
								prop.propertyType = typeof(Color);
								prop.propertyName = "backgroundColor";
							}
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

				if (binding.type == typeof(Animator))
				{
					// These seem to be magic names when exporting humanoid animation (?)
					if (memberName == "MotionT" || memberName == "MotionQ")
					{
						return false;
					}
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

		private Dictionary<(Object key, AnimationClip clip, float speed), AnimationClip> _sampledClipInstanceCache = new Dictionary<(Object, AnimationClip, float), AnimationClip>();

		private bool ClipRequiresSampling(AnimationClip clip, Transform transform)
		{
			var clipRequiresSampling = clip.isHumanMotion;

			// we also need to bake if this Animator uses animation rigging for dynamic motion
			var haveAnyRigComponents = transform.GetComponents<IAnimationWindowPreview>().Any(x => ((Behaviour)x).enabled);
			if (haveAnyRigComponents) clipRequiresSampling = true;

			return clipRequiresSampling;
		}

		private void ConvertClipToGLTFAnimation(AnimationClip clip, Transform transform, GLTFAnimation animation, float speed)
		{
			convertClipToGLTFAnimationMarker.Begin();

			// Generate GLTF.Schema.AnimationChannel and GLTF.Schema.AnimationSampler
			// 1 channel per node T/R/S, one sampler per node T/R/S
			// Need to keep a list of nodes to convert to indexes

			// Special case for animated humanoids: we also need to cache transform-to-humanoid and make sure that individual clips are used there.
			// since we're baking humanoids, we'd otherwise end up with the same clip being applied to different rigs;
			// in the future, we may want to support a system like VRM or EXT_skin_humanoid (https://github.com/takahirox/EXT_skin_humanoid) and support runtime retargeting of animations.
			if (ClipRequiresSampling(clip, transform))
			{
				var animator = transform.GetComponent<Animator>();
				var avatar = animator ? animator.avatar : null;

				if (clip.isHumanMotion && !avatar)
				{
					Debug.LogWarning(null, $"No avatar found on animated humanoid, skipping humanoid animation export on {transform.name}", transform);
					convertClipToGLTFAnimationMarker.End();
					return;
				}
				
				Object instanceCacheKey = avatar;

				// TODO fully correct would be to use all enabled components as key here
#if UNITY_2020_2_OR_NEWER
				var rig = transform.GetComponent<IAnimationWindowPreview>() as Behaviour;
				if (rig && rig.enabled) instanceCacheKey = rig;
#endif
				
				var key = (instanceCacheKey, clip, speed);
				if (!_sampledClipInstanceCache.ContainsKey(key))
				{
					var clonedClip = Object.Instantiate(clip);
					clonedClip.name = clip.name;
					_sampledClipInstanceCache.Add(key, clonedClip);
				}

				clip = _sampledClipInstanceCache[key];
			}

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

						if (UseAnimationPointer)
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
								var obj = animationPointer.animatedObject;
								Transform animatedTransform = default;
								if (obj is Component comp) animatedTransform = comp.transform;
								else if (obj is GameObject go) animatedTransform = go.transform;
								if (animatedTransform == alreadyExportedTransform)
								{
									if (targetTrShouldNotBeExported)
									{
										// Debug.LogWarning("Need to remove this", null);
									}
									else
									{
										if (animationPointer.animatedObject is GameObject)
										{
											animationPointer.animatedObject = targetTr.gameObject;
											animationPointer.channel = existingTarget;
											animationPointerResolver.Add(animationPointer);
										}
										else if(animationPointer.animatedObject is Component)
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
					if (targetTr)
						_clipAndSpeedAndPathToExportedTransform.Add((clip, speed, target), targetTr);

					var curve = targetCurvesBinding[target];
					var speedMultiplier = Mathf.Clamp(speed, 0.01f, Mathf.Infinity);

					// Initialize data
					// Bake and populate animation data
					float[] times = null;

					// arbitrary properties require the KHR_animation_pointer extension
					bool sampledAnimationData = false;
					if (UseAnimationPointer && curve.propertyCurves != null && curve.propertyCurves.Count > 0)
					{
						var curves = curve.propertyCurves;
						foreach (KeyValuePair<string, PropertyCurve> c in curves)
						{
							var prop = c.Value;
							if (BakePropertyAnimation(prop, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values))
							{
								AddAnimationData(prop.target, prop.propertyName, animation, times, values);
								sampledAnimationData = true;
							}
						}
					}

					if (targetTr)
					{
						// TODO these should be moved into curve.propertyCurves as well
						// TODO should filter by possible propertyCurve string names at that point to avoid
						// moving KHR_animation_pointer data into regular animations
						if (curve.translationCurves.Any(x => x != null))
						{
							var trp2 = new PropertyCurve(targetTr, "translation") { propertyType = typeof(Vector3) };
							trp2.curve.AddRange(curve.translationCurves);
							if (BakePropertyAnimation(trp2, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values2))
							{
								AddAnimationData(targetTr, trp2.propertyName, animation, times, values2);
								sampledAnimationData = true;
							}
						}

						if (curve.rotationCurves.Any(x => x != null))
						{
							var trp3 = new PropertyCurve(targetTr, "rotation") { propertyType = typeof(Quaternion) };
							trp3.curve.AddRange(curve.rotationCurves.Where(x => x != null));
							if (BakePropertyAnimation(trp3, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values3))
							{
								AddAnimationData(targetTr, trp3.propertyName, animation, times, values3);
								sampledAnimationData = true;
							}

						}

						if (curve.scaleCurves.Any(x => x != null))
						{
							var trp4 = new PropertyCurve(targetTr, "scale") { propertyType = typeof(Vector3) };
							trp4.curve.AddRange(curve.scaleCurves);
							if (BakePropertyAnimation(trp4, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values4))
							{
								AddAnimationData(targetTr, trp4.propertyName, animation, times, values4);
								sampledAnimationData = true;
							}
						}

						if (curve.weightCurves.Any(x => x.Value != null))
						{
							var trp5 = new PropertyCurve(targetTr, "weights") { propertyType = typeof(float) };
							trp5.curve.AddRange(curve.weightCurves.Values);
							if (BakePropertyAnimation(trp5, clip.length, AnimationBakingFramerate, speedMultiplier, out times, out var values5))
							{
								var targetComponent = targetTr.GetComponent<SkinnedMeshRenderer>();
								AddAnimationData(targetComponent, trp5.propertyName, animation, times, values5);
								sampledAnimationData = true;
							}
						}
					}

					if (!sampledAnimationData)
						Debug.LogWarning("Warning: empty animation curves for " + target + " in " + clip + " from " + transform, transform);
				}
			}
			else
			{
				Debug.LogError("Only baked animation is supported for now. Skipping animation", null);
			}

			convertClipToGLTFAnimationMarker.End();
		}

		private static readonly string[] _humanoidMotionPropertyNames = new string[] { "x", "y", "z", "w" };
		private static string[] _humanoidMuscleNames = null;
		private void CollectClipCurves(GameObject root, AnimationClip clip, Dictionary<string, TargetCurveSet> targetCurves)
		{
#if UNITY_EDITOR
			// The clip sampling only gets transform data
			// but if we have both transform and blend shape animations
			// then we need to continue to sample the blend shape curves
			var didSampleCurves = false;
			if (ClipRequiresSampling(clip, root.transform))
			{
				didSampleCurves = true;
				CollectClipCurvesBySampling(root, clip, targetCurves);
			}

			foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
			{
				var containsPosition = binding.propertyName.Contains("m_LocalPosition");
				var containsScale = binding.propertyName.Contains("m_LocalScale");
				var containsRotation = binding.propertyName.ToLowerInvariant().Contains("localrotation");
				var containsEuler = binding.propertyName.ToLowerInvariant().Contains("localeuler");

				if (didSampleCurves)
				{
					// If the animation was already collected by sampling the transform we don't care for any transform data anymore, otherwise we have it twice
					if (containsPosition || containsScale || containsRotation || containsEuler)
						continue;

					// There's also a bunch of properties from humanoid animation that we want to skip if this is a humanoid animation
					if (clip.isHumanMotion)
					{
						var parts = binding.propertyName.Split('.');

						// IK Targets and Root Motion
						if (parts.Length == 2)
						{
							var extraNames = new[] { "MotionT", "MotionQ", "RootT", "RootQ", "LeftFootT", "LeftFootQ", "RightFootT", "RightFootQ", "LeftHandT", "LeftHandQ", "RightHandT", "RightHandQ" };
							if (extraNames.Contains(parts[0]) && _humanoidMotionPropertyNames.Contains(parts[1]))
								continue;
						}
						// Muscle Animation
						if (_humanoidMuscleNames == null)
						{
							var list = new MuscleHandle[MuscleHandle.muscleHandleCount];
							MuscleHandle.GetMuscleHandles(list);
							_humanoidMuscleNames = list.Select(x => x.name).ToArray();
						}

						if (_humanoidMuscleNames.Contains(binding.propertyName))
							continue;
					}
				}

				var containsBlendShapeWeight = binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal);
				var containsCompatibleData = containsPosition || containsScale || containsRotation || containsEuler || containsBlendShapeWeight;

				if (!containsCompatibleData && !UseAnimationPointer)
				{
					Debug.LogWarning("No compatible animation data found in clip binding: " + binding.propertyName + ". You may want to turn KHR_animation_pointer export on.", clip);
					continue;
				}

				AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);

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
				else if (UseAnimationPointer)
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

			// object reference curves - in some cases animated data can be contained in here, e.g. for SpriteRenderers.
			// this only makes sense when AnimationPointer is on, and someone needs to resolve the data to something in the glTF later via KHR_animation_pointer_Resolver
			if (UseAnimationPointer)
			{
				var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
				foreach (var binding in objectBindings)
				{
					var obj = AnimationUtility.GetAnimatedObject(root, binding);
					switch (obj)
					{
						case SpriteRenderer spriteRenderer:
							if (!spriteRenderer.sprite) continue;
							if (binding.propertyName != "m_Sprite") continue;

							var spriteSheet = spriteRenderer.sprite;
							var spriteSheetPath = AssetDatabase.GetAssetPath(spriteSheet);
							// will only work with all sprites from the same spritesheet right now
							var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteSheetPath);
							if (sprites.Length == 1)
							{
								Debug.LogWarning(
									$"Spritesheet animation {spriteSheet.name} has only one frame. Exporting sprite animation using multiple image files is currently not supported. If you intent to export animated sprites please provide a spritesheet containing all your frames.",
									spriteRenderer);
							}

							var path = binding.propertyName;
							if (!targetCurves.ContainsKey(path))
							{
								var curveSet = new TargetCurveSet();
								curveSet.Init();
								targetCurves.Add(path, curveSet);
							}

							TargetCurveSet current = targetCurves[path];
							var objectKeys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
							var curve = new AnimationCurve();
							var keyframes = new List<Keyframe>();
							var lastKeyframe = default(Keyframe);
							for (var index = 0; index < objectKeys.Length; index++)
							{
								var objectKey = objectKeys[index];
								var spriteIndex = objectKeys[index].value ? Array.IndexOf(sprites, objectKeys[index].value) : 0;
								var kf = new Keyframe(objectKey.time, spriteIndex);

								// create intermediate keyframe to make sure we dont have interpolation between sprites
								// TODO better would be to allow configuring a constant track
								if ((int)lastKeyframe.value != (int)kf.value)
								{
									var intermediate = new Keyframe(kf.time - 0.0001f, lastKeyframe.value);
									keyframes.Add(intermediate);
								}
								keyframes.Add(kf);
								lastKeyframe = kf;
							}
							curve.keys = keyframes.ToArray();
							// Ensure that the curve is constant for spritesheet animation
							for (var i = 0; i < curve.keys.Length; i++)
							{
								AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
							}
							current.AddPropertyCurves(obj, curve, binding);
							targetCurves[path] = current;

							break;
					}
				}
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

				if (current.propertyCurves?.Count > 0)
				{
					foreach (var kvp in current.propertyCurves)
					{
						var prop = kvp.Value;
						if (prop.propertyType == typeof(Color))
						{
							var memberName = prop.propertyName;
							if (TryGetCurrentValue(prop.target, memberName, out var value))
							{
								// Generate missing color channels (so an animated color has always keyframes for all 4 channels)

								var col = (Color)value;

								var hasRedChannel = prop.FindIndex(v => v.EndsWith(".r")) >= 0;
								var hasGreenChannel = prop.FindIndex(v => v.EndsWith(".g")) >= 0;
								var hasBlueChannel = prop.FindIndex(v => v.EndsWith(".b")) >= 0;
								var hasAlphaChannel = prop.FindIndex(v => v.EndsWith(".a")) >= 0;

								if (!hasRedChannel) AddMissingCurve(memberName + ".r", col.r);
								if (!hasGreenChannel) AddMissingCurve(memberName + ".g", col.g);
								if (!hasBlueChannel) AddMissingCurve(memberName + ".b", col.b);
								if (!hasAlphaChannel) AddMissingCurve(memberName + ".a", col.a);

								void AddMissingCurve(string curveName, float constantValue)
								{
									var curve = CreateConstantCurve(constantValue, endTime);
									prop.curve.Add(curve);
									prop.curveName.Add(curveName);
								}
							}
						}
					}
				}

				targetCurvesBinding[target] = current;
			}
		}

		private static readonly Dictionary<(Type type, string name), MemberInfo> memberCache = new Dictionary<(Type type, string name), MemberInfo>();
		private static bool TryGetCurrentValue(object instance, string memberName, out object value)
		{
			if (instance == null || memberName == null)
			{
				value = null;
				return false;
			}

			var key = (instance.GetType(), memberName);
			if (!memberCache.TryGetValue(key, out var member))
			{
				var type = instance.GetType();
				while (type != null)
				{
					member = type
						.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.FirstOrDefault();
					if (member != null)
					{
						memberCache.Add(key, member);
						break;
					}
					type = type.BaseType;
				}
			}

			if (member == null)
			{
				value = null;
				return false;
			}

			switch (member)
			{
				case FieldInfo field:
					value = field.GetValue(instance);
					return true;
				case PropertyInfo property:
					value = property.GetValue(instance);
					return true;
				default:
					value = null;
					return false;
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

			prop.SortCurves();
			if (!prop.Validate()) return false;

			var nbSamples = Mathf.Max(1, Mathf.CeilToInt(length * bakingFramerate));
			var deltaTime = length / nbSamples;

			var _times = new List<float>(nbSamples * 2);
			var _values = new List<object>(nbSamples * 2);

			var curveCount = prop.curve.Count;
			var keyframes = prop.curve.Select(x => x.keys).ToArray();
			var keyframeIndex = new int[curveCount];
			
			// Assuming all the curves exist now
			for (var i = 0; i < nbSamples; ++i)
			{
				var time = i * deltaTime;
				if (i == nbSamples - 1) time = length;

				for (var k = 0; k < curveCount; k++)
					while (keyframeIndex[k] < keyframes[k].Length - 1 && keyframes[k][keyframeIndex[k]].time < time)
						keyframeIndex[k]++;

				var isConstant = false;
				for (var k = 0; k < curveCount; k++)
					isConstant |= float.IsInfinity(keyframes[k][keyframeIndex[k]].inTangent);

				if (isConstant && _times.Count > 0)
				{
					var lastTime = _times[_times.Count - 1];
					var t0 = lastTime + 0.0001f;
					if (i != nbSamples - 1)
						time += deltaTime * 0.999f;
					_times.Add(t0 / speedMultiplier);
					_times.Add(time / speedMultiplier);
					var success = AddValue(time);
					success &= AddValue(time);
					if (!success) return false;
				}
				else
				{
					var t0 = time / speedMultiplier;
					_times.Add(t0);
					if (!AddValue(t0)) return false;
				}

				bool AddValue(float t)
				{
					if (prop.curve.Count == 1)
					{
						var value = prop.curve[0].Evaluate(t);
						_values.Add(value);
					}
					else
					{
						var type = prop.propertyType;

						if (typeof(Vector2) == type)
						{
							_values.Add(new Vector2(prop.Evaluate(t, 0), prop.Evaluate(t, 1)));
						}
						else if (typeof(Vector3) == type)
						{
							var vec = new Vector3(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2));
							_values.Add(vec);
						}
						else if (typeof(Vector4) == type)
						{
							_values.Add(new Vector4(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3)));
						}
						else if (typeof(Color) == type)
						{
							var r = prop.Evaluate(t, 0);
							var g = prop.Evaluate(t, 1);
							var b = prop.Evaluate(t, 2);
							var a = prop.Evaluate(t, 3);
							_values.Add(new Color(r, g, b, a));
						}
						else if (typeof(Quaternion) == type)
						{
							if (prop.curve.Count == 3)
							{
								Quaternion eulerToQuat = Quaternion.Euler(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2));
								_values.Add(new Quaternion(eulerToQuat.x, eulerToQuat.y, eulerToQuat.z, eulerToQuat.w));
							}
							else if (prop.curve.Count == 4)
							{
								_values.Add(new Quaternion(prop.Evaluate(t, 0), prop.Evaluate(t, 1), prop.Evaluate(t, 2), prop.Evaluate(t, 3)));
							}
							else
							{
								Debug.LogError(null, $"Rotation animation has {prop.curve.Count} curves, expected Euler Angles (3 curves) or Quaternions (4 curves). This is not supported, make sure to animate all components of rotations. Animated object {prop.target}", prop.target);
							}
						}
						else if (typeof(float) == type)
						{
							foreach (var val in prop.curve)
								_values.Add(val.Evaluate(t));
						}
						else
						{
							switch (prop.propertyName)
							{
								case "MotionT":
								case "MotionQ":
									// Ignore
									break;
								default:
									Debug.LogWarning(null, "Property is animated but can't be exported - Name: " + prop.propertyName + ", Type: " + prop.propertyType + ". Does its target exist? You can enable KHR_animation_pointer export in the Project Settings to export more animated properties.");
									break;

							}
							return false;
						}
					}

					return true;
				}
			}

			times = _times.ToArray();
			values = _values.ToArray();

			RemoveUnneededKeyframes(ref times, ref values);

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

		private bool ArrayRangeEquals(object[] array, int sectionLength, int lastExportedSectionStart, int prevSectionStart, int sectionStart, int nextSectionStart)
		{
			var equals = true;
			for (int i = 0; i < sectionLength; i++)
			{
				equals &= (lastExportedSectionStart >= prevSectionStart || array[lastExportedSectionStart + i].Equals(array[sectionStart + i])) &&
				          array[prevSectionStart + i].Equals(array[sectionStart + i]) &&
				          array[sectionStart + i].Equals(array[nextSectionStart + i]);
				if (!equals) return false;
			}

			return true;
		}

		public void RemoveUnneededKeyframes(ref float[] times, ref object[] values)
		{
			if (times.Length <= 1)
				return;

			removeAnimationUnneededKeyframesMarker.Begin();

			var t2 = new List<float>(times.Length);
			var v2 = new List<object>(values.Length);

			var arraySize = values.Length / times.Length;

			if (arraySize == 1)
			{
				t2.Add(times[0]);
				v2.Add(values[0]);

				int lastExportedIndex = 0;
				for (int i = 1; i < times.Length - 1; i++)
				{
					removeAnimationUnneededKeyframesCheckIdenticalMarker.Begin();
					var isIdentical = (lastExportedIndex >= i - 1 || values[lastExportedIndex].Equals(values[i])) && values[i - 1].Equals(values[i]) && values[i].Equals(values[i + 1]);
					if (!isIdentical)
					{
						lastExportedIndex = i;
						t2.Add(times[i]);
						v2.Add(values[i]);
					}
					removeAnimationUnneededKeyframesCheckIdenticalMarker.End();
				}

				var max = times.Length - 1;
				t2.Add(times[max]);
				v2.Add(values[max]);
			}
			else
			{
				var singleFrameWeights = new object[arraySize];
				Array.Copy(values, 0, singleFrameWeights, 0, arraySize);
				t2.Add(times[0]);
				v2.AddRange(singleFrameWeights);

				int lastExportedIndex = 0;
				for (int i = 1; i < times.Length - 1; i++)
				{
					removeAnimationUnneededKeyframesCheckIdenticalMarker.Begin();
					var isIdentical = ArrayRangeEquals(values, arraySize, lastExportedIndex * arraySize, (i - 1) * arraySize, i * arraySize, (i + 1) * arraySize);
					if (!isIdentical)
					{
						Array.Copy(values, (i - 1) * arraySize, singleFrameWeights, 0, arraySize);
						v2.AddRange(singleFrameWeights);
						t2.Add(times[i]);
					}

					removeAnimationUnneededKeyframesCheckIdenticalMarker.End();
				}

				var max = times.Length - 1;
				t2.Add(times[max]);
				var skipped = values.Skip((max - 1) * arraySize).ToArray();
				v2.AddRange(skipped.Take(arraySize));
			}

			times = t2.ToArray();
			values = v2.ToArray();

			removeAnimationUnneededKeyframesMarker.End();
		}
	}
}
