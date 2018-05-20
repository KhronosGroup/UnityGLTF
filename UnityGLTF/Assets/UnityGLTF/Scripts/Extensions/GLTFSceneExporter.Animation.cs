using System;
using System.Collections.Generic;
using GLTF.Schema;
using UnityEngine;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public partial class GLTFSceneExporter
	{
		private enum Property
		{
			m_LocalPosition = GLTFAnimationChannelPath.translation,
			m_LocalRotation = GLTFAnimationChannelPath.rotation,
			m_LocalScale = GLTFAnimationChannelPath.scale,
			blendShape = GLTFAnimationChannelPath.weights,
		}

		//holds already exported animation clips
		private readonly Dictionary<object, int> _animationCache = new Dictionary<object, int>();

		private void ExportAnimations(IEnumerable<Transform> transforms)
		{
			foreach (Transform transform in transforms)
			{
				//use Animator instead of Animation for support of newest features
				GameObject gameObject = transform.gameObject;
				foreach (Animator animator in gameObject.GetComponentsInChildren<Animator>())
				{
					AnimatorController animatorController = (AnimatorController)animator.runtimeAnimatorController;
					if (animatorController == null)
					{
						//no animations attached
						continue;
					}

					foreach (AnimatorControllerLayer layer in animatorController.layers)
					{
						ChildAnimatorState[] animationStates = layer.stateMachine.states;
						foreach (ChildAnimatorState childState in animationStates)
						{
							AnimatorState state = childState.state;
							if (state.motion is BlendTree)
							{
								throw new NotSupportedException();
							}

							AnimationClip animationClip = (AnimationClip)state.motion;
							if (animationClip != null)
							{
								//export animation clip from each state of the state machine
								ExportAnimation(gameObject, animationClip);
							}
						}
					}
				}
			}
		}

		private void ExportAnimation(GameObject gameObject, AnimationClip unityAnimationClip)
		{
			int index;
			if (_animationCache.TryGetValue(unityAnimationClip, out index))
			{
				//already exported this clip, skip
				return;
			}

			List<AnimationChannel> channels = new List<AnimationChannel>();
			List<AnimationSampler> samplers = new List<AnimationSampler>();

			//using the curve bindings given by the editor, construct a map from target object (path), to the animated property, then to the corresponding animated curves (members)
			foreach (var pairPath in GroupAnimationCurveBindings(AnimationUtility.GetCurveBindings(unityAnimationClip)))
			{
				//pairPath: KEY = path -> VALUE = (property -> member curve)
				string path = pairPath.Key;
				Dictionary<string, Dictionary<string, EditorCurveBinding>> propertyCurves = pairPath.Value;

				//pairProperty: KEY = property -> VALUE = member curve
				foreach (var pairProperty in propertyCurves)
				{
					//object to be animated
					Debug.Log(path);
					GameObject targetGameObject = gameObject.transform.Find(path).gameObject;
					Property property = (Property)Enum.Parse(typeof(Property), pairProperty.Key);
					Dictionary<string, EditorCurveBinding> memberCurves = pairProperty.Value;
					
					SamplerId samplerId = new SamplerId()
					{
						Id = samplers.Count,
						Root = _root,
					};
					AnimationSampler exportedSampler = ExportProperty(unityAnimationClip, targetGameObject, property, memberCurves);
					samplers.Add(exportedSampler);

					//new channel: holds created sampler, and a new target
					channels.Add(new AnimationChannel
					{
						Sampler = samplerId,
						Target = new AnimationChannelTarget
						{
							Node = _nodeCache[targetGameObject.transform],
							Path = (GLTFAnimationChannelPath) property,
						}
					});
				}
			}

			//exported channels and samplers for this animation clip, cache it and attach to GLTF root
			index = _root.Animations.Count;
			_animationCache.Add(unityAnimationClip, index);

			var animation = new GLTF.Schema.Animation()
			{
				Name = unityAnimationClip.name,
				Channels = channels,
				Samplers = samplers
			};
			
			_root.Animations.Add(animation);
		}

		//identifies the animation property to be exported, and exports it as a sampler
		private AnimationSampler ExportProperty(AnimationClip unityAnimationClip, GameObject targetGameObject, Property property,
																							Dictionary<string, EditorCurveBinding> memberCurves)
		{
			switch (property)
			{
				case Property.m_LocalPosition:
					return ExportAnimationSamplerPosition(
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["x"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["y"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["z"]));

				case Property.m_LocalScale:
					return ExportAnimationSamplerScale(
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["x"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["y"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["z"]));

				case Property.m_LocalRotation:
					return ExportAnimationSamplerRotation(
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["x"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["y"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["z"]),
						AnimationUtility.GetEditorCurve(unityAnimationClip, memberCurves["w"]));

				case Property.blendShape:
					return ExportSamplerWeightProperty(unityAnimationClip, targetGameObject, memberCurves);

				default:
					throw new NotSupportedException();
			}
		}

		//creates the weight curve by using the morph targets associated with the target animated object
		private AnimationSampler ExportSamplerWeightProperty(AnimationClip unityAnimationClip, GameObject targetGameObject, Dictionary<string, EditorCurveBinding> memberCurves)
		{
			SkinnedMeshRenderer skinnedMeshRenderer = targetGameObject.GetComponent<SkinnedMeshRenderer>();
			UnityEngine.Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
			//weight curves to build
			AnimationCurve[] weightCurves = new AnimationCurve[sharedMesh.blendShapeCount];
			for (int i = 0; i < sharedMesh.blendShapeCount; i++)
			{
				string blendShapeName = sharedMesh.GetBlendShapeName(i);

				EditorCurveBinding binding;
				if (memberCurves.TryGetValue(blendShapeName, out binding))
				{
					//already seen the weight curve, use it
					weightCurves[i] = AnimationUtility.GetEditorCurve(unityAnimationClip, binding);
				}
				else
				{
					float blendShapeWeight = skinnedMeshRenderer.GetBlendShapeWeight(i);
					weightCurves[i] = AnimationCurve.Linear(0, blendShapeWeight, unityAnimationClip.length, blendShapeWeight);
				}
			}
			return ExportAnimationSamplerWeight(weightCurves);
		}

		//Parses the Unity Editor animation curve bindings in a map datastructure that maps from path to animated object, to animated property, to animated curves
		private Dictionary<string, Dictionary<string, Dictionary<string, EditorCurveBinding>>> GroupAnimationCurveBindings(IEnumerable<EditorCurveBinding> inputBindings)
		{
			//an animation clip defines a curve that describes the change in a property of some object over time
			//path: object to be animated --- property: property (TRS, Weights) animated --- member: corresponding curve
			//member is defined as a dictionary since each curve of each property might animate several components (x, y, z, w)
			var outBindings = new Dictionary<string, Dictionary<string, Dictionary<string, EditorCurveBinding>>>();

			foreach (EditorCurveBinding inputBinding in inputBindings)
			{
				Dictionary<string, Dictionary<string, EditorCurveBinding>> propertyBindings;
				if (!outBindings.TryGetValue(inputBinding.path, out propertyBindings))
				{
					//if the key (path of the animated object) is not already in the map, add it
					propertyBindings = new Dictionary<string, Dictionary<string, EditorCurveBinding>>();
					outBindings.Add(inputBinding.path, propertyBindings);
				}

				//propertyName is formatted as follows in Unity: property-member (e.g m_localScale-x)
				string[] split = inputBinding.propertyName.Split(new char[] { '.' }, 2);
				string property = split[0];

				Dictionary<string, EditorCurveBinding> memberBindings;
				if (!propertyBindings.TryGetValue(property, out memberBindings))
				{
					memberBindings = new Dictionary<string, EditorCurveBinding>();
					propertyBindings.Add(property, memberBindings);
				}

				string member = split[1];
				memberBindings.Add(member, inputBinding);
			}

			return outBindings;
		}

		private AnimationSampler ExportAnimationSamplerPosition(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
		{
			
			return ExportAnimationSampler(
				new AnimationCurve[] { curveX, curveY, curveZ },
				keyIndex => GetRightHandedVector(new Vector3(curveX.keys[keyIndex].inTangent, curveY.keys[keyIndex].inTangent, curveZ.keys[keyIndex].inTangent)),
				keyIndex => GetRightHandedVector(new Vector3(curveX.keys[keyIndex].value, curveY.keys[keyIndex].value, curveZ.keys[keyIndex].value)),
				keyIndex => GetRightHandedVector(new Vector3(curveX.keys[keyIndex].outTangent, curveY.keys[keyIndex].outTangent, curveZ.keys[keyIndex].outTangent)),
				time => GetRightHandedVector(new Vector3(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time))),
				values => GLTF.DataExporter.ExportData(values.Select(value => value.ToGltfVector3Convert()), _bufferId, _root, _bufferWriter));
		}

		private static Quaternion CreateNormalizedQuaternion(float x, float y, float z, float w)
		{
			var factor = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
			return new Quaternion(x * factor, y * factor, z * factor, w * factor);
		}

		private AnimationSampler ExportAnimationSamplerRotation(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
		{
			return ExportAnimationSampler(
				new AnimationCurve[] { curveX, curveY, curveZ, curveW },
				keyIndex => GetRightHandedQuaternion(new Quaternion(curveX.keys[keyIndex].inTangent, curveY.keys[keyIndex].inTangent, curveZ.keys[keyIndex].inTangent, curveW.keys[keyIndex].inTangent)),
				keyIndex => GetRightHandedQuaternion(CreateNormalizedQuaternion(curveX.keys[keyIndex].value, curveY.keys[keyIndex].value, curveZ.keys[keyIndex].value, curveW.keys[keyIndex].value)),
				keyIndex => GetRightHandedQuaternion(new Quaternion(curveX.keys[keyIndex].outTangent, curveY.keys[keyIndex].outTangent, curveZ.keys[keyIndex].outTangent, curveW.keys[keyIndex].outTangent)),
				time => GetRightHandedQuaternion(CreateNormalizedQuaternion(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time), curveW.Evaluate(time))),
				values => GLTF.DataExporter.ExportData(values.Select(value => value.ToGltfQuaternionConvert()), _bufferId, _root, _bufferWriter));
		}

		private AnimationSampler ExportAnimationSamplerScale(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
		{
			return ExportAnimationSampler(
				new AnimationCurve[] { curveX, curveY, curveZ },
				keyIndex => new Vector3(curveX.keys[keyIndex].inTangent, curveY.keys[keyIndex].inTangent, curveZ.keys[keyIndex].inTangent),
				keyIndex => new Vector3(curveX.keys[keyIndex].value, curveY.keys[keyIndex].value, curveZ.keys[keyIndex].value),
				keyIndex => new Vector3(curveX.keys[keyIndex].outTangent, curveY.keys[keyIndex].outTangent, curveZ.keys[keyIndex].outTangent),
				time => new Vector3(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time)),
				values => GLTF.DataExporter.ExportData(values.Select(value => value.ToGltfVector3Convert()), _bufferId, _root, _bufferWriter));
		}

		private AnimationSampler ExportAnimationSamplerWeight(IEnumerable<AnimationCurve> weightCurves)
		{
			return ExportAnimationSampler(
				weightCurves,
				keyIndex => weightCurves.Select(curve => curve.keys[keyIndex].inTangent / 100),
				keyIndex => weightCurves.Select(curve => curve.keys[keyIndex].value / 100),
				keyIndex => weightCurves.Select(curve => curve.keys[keyIndex].outTangent / 100),
				time => weightCurves.Select(curve => curve.Evaluate(time) / 100),
				values => GLTF.DataExporter.ExportData(values.SelectMany(value => value), _bufferId, _root, _bufferWriter));
		}

		private AnimationSampler ExportAnimationSampler<T>(IEnumerable<AnimationCurve> curves, Func<int, T> getInTangent, Func<int, T> getValue, Func<int, T> getOutTangent, Func<float, T> evaluate, Func<IEnumerable<T>, AccessorId> exportData)
		{
			//decide if we should include baking animations or not
			if (/*bakeAnimations is False &&*/ CanExportCurvesAsSpline(curves))
			{
				var firstCurve = curves.First();

				var input = new float[firstCurve.keys.Length];
				var output = new T[firstCurve.keys.Length * 3];
				for (int keyIndex = 0; keyIndex < firstCurve.keys.Length; keyIndex++)
				{
					//input: the keyframes -- input of the sampler has the same length as the first curve
					input[keyIndex] = firstCurve.keys[keyIndex].time;

					//tangent when approaching the output from the previous point in the curve
					output[keyIndex * 3 + 0] = getInTangent(keyIndex);
					//value of the output at the keyframe
					output[keyIndex * 3 + 1] = getValue(keyIndex);
					//tangent when leaving the value at the keyframe, approaching the next point on the curve
					output[keyIndex * 3 + 2] = getOutTangent(keyIndex);
				}

				return new AnimationSampler
				{
					Input = GLTF.DataExporter.ExportData(input, _bufferId, _root, _bufferWriter, minMax: true),
					Interpolation = GLTF.Schema.InterpolationType.CUBICSPLINE,
					Output = exportData(output),
				};
			}
			else
			{
				var input = new List<float>();
				var output = new List<T>();
				var maxTime = curves.Max(curve => curve.keys.Last().time);
				for (float time = 0; time < maxTime; time += 1.0f / 30.0f)
				{
					input.Add(time);
					output.Add(evaluate(time));
				}

				return new AnimationSampler
				{
					Input = GLTF.DataExporter.ExportData(input, _bufferId, _root, _bufferWriter, minMax: true),
					Interpolation = GLTF.Schema.InterpolationType.LINEAR,
					Output = exportData(output),
				};
			}
		}

		private static bool CanExportCurvesAsSpline(IEnumerable<AnimationCurve> curves)
		{
			AnimationCurve firstCurve = curves.First();
			IEnumerable<AnimationCurve> remainingCurves = curves.Skip(1);

			// All curves must have the same number of keys.
			if (!remainingCurves.All(curve => curve.keys.Length == firstCurve.keys.Length))
			{
				return false;
			}

			for (int keyIndex = 0; keyIndex < firstCurve.keys.Length; keyIndex++)
			{
				// All curves must have the same time values.
				if (!remainingCurves.All(curve => Mathf.Approximately(curve.keys[keyIndex].time, firstCurve.keys[keyIndex].time)))
				{
					return false;
				}
			}

			return true;
		}

		private static Vector3 GetRightHandedVector(Vector3 value)
		{
			return new Vector3(value.x, value.y, -value.z);
		}

		private static Vector4 GetRightHandedVector(Vector4 value)
		{
			return new Vector4(value.x, value.y, -value.z, -value.w);
		}

		private static Quaternion GetRightHandedQuaternion(Quaternion value)
		{
			return new Quaternion(-value.x, -value.y, value.z, value.w);
		}

	}
}
