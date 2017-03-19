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

        /// <summary>
        /// Generate Animation components from glTF animations, and attach to game objects
        /// </summary>
        public void AttachToGameObjects()
        {
            // create the animation clip that will contain animation data
            AnimationClip clip = new AnimationClip();
            clip.name = name ?? "GLTFAnimation";

            // needed because Animator component is unavailable at runtime
            clip.legacy = true;

            foreach(var channel in channels)
            {
                AnimationCurve[] sampler = samplers[channel.sampler].AsAnimationCurves();
                if(channel.target.path == GLTFAnimationChannelPath.translation)
                {
                    clip.SetCurve("", typeof(Transform), "localPosition.x", sampler[0]);
                    clip.SetCurve("", typeof(Transform), "localPosition.y", sampler[1]);
                    clip.SetCurve("", typeof(Transform), "localPosition.z", sampler[2]);
                }
                else if (channel.target.path == GLTFAnimationChannelPath.rotation)
                {
                    clip.SetCurve("", typeof(Transform), "localRotation.x", sampler[0]);
                    clip.SetCurve("", typeof(Transform), "localRotation.y", sampler[1]);
                    clip.SetCurve("", typeof(Transform), "localRotation.z", sampler[2]);
                    clip.SetCurve("", typeof(Transform), "localRotation.w", sampler[3]);
                }
                else if (channel.target.path == GLTFAnimationChannelPath.scale)
                {
                    clip.SetCurve("", typeof(Transform), "localScale.x", sampler[0]);
                    clip.SetCurve("", typeof(Transform), "localScale.y", sampler[1]);
                    clip.SetCurve("", typeof(Transform), "localScale.z", sampler[2]);
                }
                else
                {
                    Debug.LogWarning("Cannot read GLTF animation path");
                }

                GameObject target = channel.target.node.Value.AsGameObject();
                // TODO: figure out how to build relative paths for glTF nodes
            }

            // TODO: figure out what to do with the clip once it's built
        }
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
        public uint sampler;

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

        private AnimationCurve[] curves;

        /// <summary>
        /// Create AnimationCurves from glTF animation sampler data
        /// </summary>
        /// <returns>AnimationCurve[]</returns>
        public AnimationCurve[] AsAnimationCurves()
        {
            if (curves != null)
                return curves;

            float[] timeArray = input.Value.AsFloatArray();
            float[] animArray = output.Value.AsFloatArray();
            int vecSize;

            // check transform stride
            if (output.Value.type == GLTFAccessorAttributeType.VEC3)
                vecSize = 3;
            else if (output.Value.type == GLTFAccessorAttributeType.VEC4)
                vecSize = 4;
            else
                throw new GLTFTypeMismatchException("Animation sampler output points to invalidly-typed accessor");

            // check types
            if(timeArray.Length * vecSize != animArray.Length)
            {
                throw new GLTFTypeMismatchException("Animation sampler input and output accessors incompatible");
            }

            curves = new AnimationCurve[vecSize];

            for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
            {
                for(int vecIdx = 0; vecIdx < vecSize; vecIdx++)
                {
                    curves[vecIdx].AddKey(timeArray[timeIdx], animArray[vecSize * timeIdx + vecIdx]);
                }
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
