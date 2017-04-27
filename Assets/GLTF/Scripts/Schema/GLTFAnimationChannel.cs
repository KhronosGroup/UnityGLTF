using System;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Targets an animation's sampler at a node's property.
    /// </summary>
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
                    default:
                        animationChannel.DefaultPropertyDeserializer(root, reader);
                        break;
                }
            }

            return animationChannel;
        }
    }   
}
