using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace GLTF
{
    public class GLTFAnimation
    {
        /// <summary>
        /// An array of channels, each of which targets an animation's sampler at a
        /// node's property. Different channels of the same animation can't have equal
        /// targets.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAnimationChannel[] channels;

        /// <summary>
        /// An array of samplers that combines input and output accessors with an
        /// interpolation algorithm to define a keyframe graph (but not its target).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAnimationSampler[] samplers;

        public string name;
    }

    /// <summary>
    /// Targets an animation's sampler at a node's property.
    /// </summary>
    public class GLTFAnimationChannel
    {
        /// <summary>
        /// The index of a sampler in this animation used to compute the value for the
        /// target, e.g., a node's translation, rotation, or scale (TRS).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFSamplerId sampler;

        /// <summary>
        /// The index of the node and TRS property to target.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAnimationChannelTarget target;
    }

    /// <summary>
    /// The index of the node and TRS property that an animation channel targets.
    /// </summary>
    public class GLTFAnimationChannelTarget
    {
        /// <summary>
        /// The index of the node to target.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFNodeId node;

        /// <summary>
        /// The name of the node's TRS property to modify.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GLTFAnimationChannelPath path;
    }

    public enum GLTFAnimationChannelPath
    {
        translation,
        rotation,
        scale
    }

    public class GLTFAnimationSampler
    {
        /// <summary>
        /// The index of an accessor containing keyframe input values, e.g., time.
        /// That accessor must have componentType `FLOAT`. The values represent time in
        /// seconds with `time[0] >= 0.0`, and strictly increasing values,
        /// i.e., `time[n + 1] > time[n]`
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAccessorId input;

        /// <summary>
        /// Interpolation algorithm. When an animation targets a node's rotation,
        /// and the animation's interpolation is `\"LINEAR\"`, spherical linear
        /// interpolation (slerp) should be used to interpolate quaternions. When
        /// interpolation is `\"STEP\"`, animated value remains constant to the value
        /// of the first point of the timeframe, until the next timeframe.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GLTFInterpolationType interpolation;

        /// <summary>
        /// The index of an accessor, containing keyframe output values. Output and input
        /// accessors must have the same `count`. When sampler is used with TRS target,
        /// output accessor's componentType must be `FLOAT`.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAccessorId output;

        /// <summary>
        /// Create AnimationCurves from glTF animation sampler data
        /// </summary>
        /// <returns>AnimationCurve[]</returns>
        public AnimationCurve[] Create()
        {
            AnimationCurve[] curves;
            float[] timeArray = input.Value.AsFloatArray();
            float[] animArray = output.Value.AsFloatArray();

            if (output.Value.type == GLTFAccessorAttributeType.VEC3)
            {
                // check types
                if(timeArray.Length *3 != animArray.Length)
                {
                    throw new GLTFTypeMismatchException("Animation sampler input and output accessors incompatible");
                }

                curves = new AnimationCurve[3];

                for (int i = 0; i < timeArray.Length; i++)
                {
                    curves[0].AddKey(timeArray[i], animArray[3 * i]);
                    curves[1].AddKey(timeArray[i], animArray[3 * i + 1]);
                    curves[2].AddKey(timeArray[i], animArray[3 * i + 2]);
                }
            }
            else if(output.Value.type == GLTFAccessorAttributeType.VEC4)
            {
                // check types
                if (timeArray.Length * 4 != animArray.Length)
                {
                    throw new GLTFTypeMismatchException("Animation sampler input and output accessors incompatible");
                }

                curves = new AnimationCurve[4];

                for (int i = 0; i < timeArray.Length; i++)
                {
                    curves[0].AddKey(timeArray[i], animArray[4 * i]);
                    curves[1].AddKey(timeArray[i], animArray[4 * i + 1]);
                    curves[2].AddKey(timeArray[i], animArray[4 * i + 2]);
                    curves[3].AddKey(timeArray[i], animArray[4 * i + 3]);
                }
            }
            else
            {
                throw new GLTFTypeMismatchException("Animation sampler output points to invalidly-typed accessor");
            }

            return curves;
        }
    }

    public enum GLTFInterpolationType
    {
        LINEAR,
        STEP
    }

    
}
