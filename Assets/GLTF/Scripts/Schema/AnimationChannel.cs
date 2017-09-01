using System;
using Newtonsoft.Json;

namespace GLTF
{
	/// <summary>
	/// Targets an animation's sampler at a node's property.
	/// </summary>
	public class AnimationChannel : GLTFProperty
	{
		/// <summary>
		/// The index of a sampler in this animation used to compute the value for the
		/// target, e.g., a node's translation, rotation, or scale (TRS).
		/// </summary>
		public AnimationSamplerID Sampler;

		/// <summary>
		/// The index of the node and TRS property to target.
		/// </summary>
		public AnimationChannelTarget Target;

		public static AnimationChannel Deserialize(GLTFRoot root, JsonReader reader, Animation animation)
		{
			var animationChannel = new AnimationChannel();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "sampler":
						animationChannel.Sampler = AnimationSamplerID.Deserialize(root, reader, animation);
						break;
					case "target":
						animationChannel.Target = AnimationChannelTarget.Deserialize(root, reader);
						break;
					default:
						animationChannel.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return animationChannel;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			writer.WritePropertyName("sampler");
			writer.WriteValue(Sampler.Id);

			writer.WritePropertyName("target");
			Target.Serialize(writer);

			base.Serialize(writer);

			writer.WriteEndObject();
		}

		/// <summary>
		/// Create AnimationCurves from glTF animation sampler data
		/// </summary>
		/// <returns>AnimationCurve[]</returns>
		public UnityEngine.AnimationCurve[] AsAnimationCurves()
		{
			float[] timeArray = Sampler.Value.Input.Value.AsFloatArray();
			var curves = GenerateCurvesArray();
			FillCurveData(curves, timeArray);
			InputCurveInterpolation(curves);

			return curves;
		}

		private UnityEngine.AnimationCurve[] GenerateCurvesArray() {
			var node = Target.Node.Value;
			int stride;
			if (IsBlendShapes())
				stride = node.Mesh.Value.Primitives[0].Targets.Count;
			else if (Sampler.Value.Output.Value.Type == GLTFAccessorAttributeType.VEC3)
				stride = 3;
			else if (Sampler.Value.Output.Value.Type == GLTFAccessorAttributeType.VEC4)
				stride = 4;
			else
				throw new GLTFTypeMismatchException("Animation sampler output points to invalidly-typed accessor " + Sampler.Value.Output.Value.Type);

			var curves = new UnityEngine.AnimationCurve[stride];
			for (int i = 0; i < stride; i++)
				curves[i] = new UnityEngine.AnimationCurve();

			return curves;
		}

		private bool IsBlendShapes() {
			return Target.Path == GLTFAnimationChannelPath.weights;
		}

		private void FillCurveData(UnityEngine.AnimationCurve[] curves, float[] timeArray) {
			if (IsBlendShapes())
				FillBlendShapeCurveData(curves, timeArray);
			else if (Target.Path == GLTFAnimationChannelPath.translation)
				FillTranslationCurveData(curves, timeArray);
			else if (Target.Path == GLTFAnimationChannelPath.rotation)
				FillRotationCurveData(curves, timeArray);
			else if(Target.Path == GLTFAnimationChannelPath.scale) 
				FillScaleData(curves, timeArray);
		}

		private void FillBlendShapeCurveData(UnityEngine.AnimationCurve[] curves, float[] timeArray)
		{
			var node = Target.Node.Value;
			var numTargets = node.Mesh.Value.Primitives[0].Targets.Count;
			var animArray = Sampler.Value.Output.Value.AsFloatArray();

			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
				for (int targetIndex = 0; targetIndex < numTargets; targetIndex++)
					curves[targetIndex].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[numTargets * timeIdx + targetIndex]));
		}

		private void FillTranslationCurveData(UnityEngine.AnimationCurve[] curves, float[] timeArray)
		{
			var animArray = Sampler.Value.Output.Value.AsVertexArray();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
			}
		}

		private void FillRotationCurveData(UnityEngine.AnimationCurve[] curves, float[] timeArray)
		{
			var animArray = Sampler.Value.Output.Value.AsQuaternionArray();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
				curves[3].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].w));
			}
		}

		private void FillScaleData(UnityEngine.AnimationCurve[] curves, float[] timeArray)
		{
			var animArray = Sampler.Value.Output.Value.AsVector3Array();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new UnityEngine.Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
			}
		}

		private void InputCurveInterpolation(UnityEngine.AnimationCurve[] curves) {
			if (Sampler.Value.Interpolation == InterpolationType.LINEAR)
				for (int i = 0; i < curves.Length; i++)
					LinearizeCurve(curves[i]);

			if (Sampler.Value.Interpolation == InterpolationType.STEP)
				for (int i = 0; i < curves.Length; i++)
					StepCurve(curves[i]);
		}

		private void LinearizeCurve(UnityEngine.AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				UnityEngine.Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
					key.inTangent = CalculateLinearTangent(curve[timeIdx - 1].value, curve[timeIdx].value, curve[timeIdx - 1].time, curve[timeIdx].time);

				if (timeIdx + 1 < curve.length)
					key.outTangent = CalculateLinearTangent(curve[timeIdx].value, curve[timeIdx + 1].value, curve[timeIdx].time, curve[timeIdx + 1].time);

				curve.MoveKey(timeIdx, key);
			}
		}

		private void StepCurve(UnityEngine.AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				UnityEngine.Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
					key.inTangent = float.PositiveInfinity;
					
				if (timeIdx + 1 < curve.length)
					key.outTangent = float.PositiveInfinity;

				curve.MoveKey(timeIdx, key);
			}
		}

		private float CalculateLinearTangent(float valueStart, float valueEnd, float timeStart, float timeEnd)
		{
			return (valueEnd - valueStart) / (timeEnd - timeStart);
		}
	}
}
