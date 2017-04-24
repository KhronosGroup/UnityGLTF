using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Joints and matrices defining a skin.
    /// </summary>
    [System.Serializable]
    public class GLTFSkin
    {
        /// <summary>
        /// The index of the accessor containing the floating-point 4x4 inverse-bind matrices.
        /// The default is that each matrix is a 4x4 identity matrix, which implies that inverse-bind
        /// matrices were pre-applied.
        /// </summary>
        public GLTFAccessorId inverseBindMatrices;

        /// <summary>
        /// The index of the node used as a skeleton root.
        /// When undefined, joints transforms resolve to scene root.
        /// </summary>
        public GLTFNodeId skeleton;

        /// <summary>
        /// Indices of skeleton nodes, used as joints in this skin.  The array length must be the
        // same as the `count` property of the `inverseBindMatrices` accessor (when defined).
        /// </summary>
        public List<GLTFNodeId> joints;

        public static GLTFSkin Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var skin = new GLTFSkin();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "inverseBindMatrices":
                        skin.inverseBindMatrices = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "skeleton":
                        skin.skeleton = GLTFNodeId.Deserialize(root, reader);
                        break;
                    case "joints":
                        skin.joints = reader.ReadList(() => GLTFNodeId.Deserialize(root, reader));
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return skin;
        }
    }
}
