using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GLTF
{
    /// <summary>
    /// A keyframe animation.
    /// </summary>
    [System.Serializable]
    public class GLTFAnimation : GLTFChildOfRootProperty
    {
        /// <summary>
        /// An array of channels, each of which targets an animation's sampler at a
        /// node's property. Different channels of the same animation can't have equal
        /// targets.
        /// </summary>
        public List<GLTFAnimationChannel> channels;

        /// <summary>
        /// An array of samplers that combines input and output accessors with an
        /// interpolation algorithm to define a keyframe graph (but not its target).
        /// </summary>
        public List<GLTFAnimationSampler> samplers;

        public static GLTFAnimation Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var animation = new GLTFAnimation();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "channels":
                        animation.channels = reader.ReadList(() => GLTFAnimationChannel.Deserialize(root, reader));
                        break;
                    case "samplers":
                        animation.samplers = reader.ReadList(() => GLTFAnimationSampler.Deserialize(root, reader));
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return animation;

        }
    }

    /// <summary>
    /// Targets an animation's sampler at a node's property.
    /// </summary>
    [System.Serializable]
    public class GLTFAnimationChannel
    {
        /// <summary>
        /// The index of a sampler in this animation used to compute the value for the
        /// target, e.g., a node's translation, rotation, or scale (TRS).
        /// </summary>
        public GLTFSamplerId sampler;

        /// <summary>
        /// The index of the node and TRS property to target.
        /// </summary>
        public GLTFAnimationChannelTarget target;

        public static GLTFAnimationChannel Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var animationChannel = new GLTFAnimationChannel();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Animation channel must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "sampler":
                        animationChannel.sampler = GLTFSamplerId.Deserialize(root, reader);
                        break;
                    case "target":
                        animationChannel.target = GLTFAnimationChannelTarget.Deserialize(root, reader);
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                    }
            }

            return animationChannel;
        }
    }

    /// <summary>
    /// The index of the node and TRS property that an animation channel targets.
    /// </summary>
    [System.Serializable]
    public class GLTFAnimationChannelTarget
    {
        /// <summary>
        /// The index of the node to target.
        /// </summary>
        public GLTFNodeId node;

        /// <summary>
        /// The name of the node's TRS property to modify.
        /// </summary>
        public GLTFAnimationChannelPath path;

        public static GLTFAnimationChannelTarget Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var animationChannelTarget = new GLTFAnimationChannelTarget();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Animation channel target must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "node":
                        animationChannelTarget.node = GLTFNodeId.Deserialize(root, reader);
                        break;
                    case "path":
                        animationChannelTarget.path = reader.ReadStringEnum<GLTFAnimationChannelPath>();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                    }
            }

            return animationChannelTarget;
        }
    }

    public enum GLTFAnimationChannelPath
    {
        translation,
        rotation,
        scale
    }

    /// <summary>
    /// Combines input and output accessors with an interpolation algorithm to define a keyframe graph (but not its target).
    /// </summary>
    [System.Serializable]
    public class GLTFAnimationSampler
    {
        /// <summary>
        /// The index of an accessor containing keyframe input values, e.g., time.
        /// That accessor must have componentType `FLOAT`. The values represent time in
        /// seconds with `time[0] >= 0.0`, and strictly increasing values,
        /// i.e., `time[n + 1] > time[n]`
        /// </summary>
        public GLTFAccessorId input;

        /// <summary>
        /// Interpolation algorithm. When an animation targets a node's rotation,
        /// and the animation's interpolation is `\"LINEAR\"`, spherical linear
        /// interpolation (slerp) should be used to interpolate quaternions. When
        /// interpolation is `\"STEP\"`, animated value remains constant to the value
        /// of the first point of the timeframe, until the next timeframe.
        /// </summary>
        public GLTFInterpolationType interpolation;

        /// <summary>
        /// The index of an accessor, containing keyframe output values. Output and input
        /// accessors must have the same `count`. When sampler is used with TRS target,
        /// output accessor's componentType must be `FLOAT`.
        /// </summary>
        public GLTFAccessorId output;

        public static GLTFAnimationSampler Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var animationSampler = new GLTFAnimationSampler();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Animation sampler must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "input":
                        animationSampler.input = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "interpolation":
                        animationSampler.interpolation = reader.ReadStringEnum<GLTFInterpolationType>();
                        break;
                    case "output":
                        animationSampler.output = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return animationSampler;
        }
    }

    public enum GLTFInterpolationType
    {
        LINEAR,
        STEP
    }   
}
