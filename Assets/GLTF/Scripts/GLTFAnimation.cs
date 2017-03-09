using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    }

    public enum GLTFInterpolationType
    {
        LINEAR,
        STEP
    }

    
}
