using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A keyframe animation.
    /// </summary>
    [Serializable]
    public class GLTFAnimation : GLTFChildOfRootProperty
    {
        /// <summary>
        /// An array of channels, each of which targets an animation's sampler at a
        /// node's property. Different channels of the same animation can't have equal
        /// targets.
        /// </summary>
        public List<GLTFAnimationChannel> Channels;

        /// <summary>
        /// An array of samplers that combines input and output accessors with an
        /// interpolation algorithm to define a keyframe graph (but not its target).
        /// </summary>
        public List<GLTFAnimationSampler> Samplers;

        public static GLTFAnimation Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var animation = new GLTFAnimation();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "channels":
                        animation.Channels = reader.ReadList(() => GLTFAnimationChannel.Deserialize(root, reader));
                        break;
                    case "samplers":
                        animation.Samplers = reader.ReadList(() => GLTFAnimationSampler.Deserialize(root, reader));
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
    [Serializable]
    public class GLTFAnimationChannel : GLTFProperty
    {
        /// <summary>
        /// The index of a sampler in this animation used to compute the value for the
        /// target, e.g., a node's translation, rotation, or scale (TRS).
        /// </summary>
        public GLTFSamplerId Sampler;

        /// <summary>
        /// The index of the node and TRS property to target.
        /// </summary>
        public GLTFAnimationChannelTarget Target;

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
                        animationChannel.Sampler = GLTFSamplerId.Deserialize(root, reader);
                        break;
                    case "target":
                        animationChannel.Target = GLTFAnimationChannelTarget.Deserialize(root, reader);
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
    [Serializable]
    public class GLTFAnimationChannelTarget : GLTFProperty
    {
        /// <summary>
        /// The index of the node to target.
        /// </summary>
        public GLTFNodeId Node;

        /// <summary>
        /// The name of the node's TRS property to modify.
        /// </summary>
        public GLTFAnimationChannelPath Path;

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
                        animationChannelTarget.Node = GLTFNodeId.Deserialize(root, reader);
                        break;
                    case "path":
                        animationChannelTarget.Path = reader.ReadStringEnum<GLTFAnimationChannelPath>();
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
    [Serializable]
    public class GLTFAnimationSampler : GLTFProperty
    {
        /// <summary>
        /// The index of an accessor containing keyframe input values, e.g., time.
        /// That accessor must have componentType `FLOAT`. The values represent time in
        /// seconds with `time[0] >= 0.0`, and strictly increasing values,
        /// i.e., `time[n + 1] > time[n]`
        /// </summary>
        public GLTFAccessorId Input;

        /// <summary>
        /// Interpolation algorithm. When an animation targets a node's rotation,
        /// and the animation's interpolation is `\"LINEAR\"`, spherical linear
        /// interpolation (slerp) should be used to interpolate quaternions. When
        /// interpolation is `\"STEP\"`, animated value remains constant to the value
        /// of the first point of the timeframe, until the next timeframe.
        /// </summary>
        public GLTFInterpolationType Interpolation;

        /// <summary>
        /// The index of an accessor, containing keyframe output values. Output and input
        /// accessors must have the same `count`. When sampler is used with TRS target,
        /// output accessor's componentType must be `FLOAT`.
        /// </summary>
        public GLTFAccessorId Output;

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
                        animationSampler.Input = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "interpolation":
                        animationSampler.Interpolation = reader.ReadStringEnum<GLTFInterpolationType>();
                        break;
                    case "output":
                        animationSampler.Output = GLTFAccessorId.Deserialize(root, reader);
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
