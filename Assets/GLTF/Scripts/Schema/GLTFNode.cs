using UnityEngine;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A node in the node hierarchy.
    /// When the node contains `skin`, all `mesh.primitives` must contain `JOINT`
    /// and `WEIGHT` attributes.  A node can have either a `matrix` or any combination
    /// of `translation`/`rotation`/`scale` (TRS) properties.
    /// TRS properties are converted to matrices and postmultiplied in
    /// the `T * R * S` order to compose the transformation matrix;
    /// first the scale is applied to the vertices, then the rotation, and then
    /// the translation. If none are provided, the transform is the identity.
    /// When a node is targeted for animation
    /// (referenced by an animation.channel.target), only TRS properties may be present;
    /// `matrix` will not be present.
    /// </summary>
    public class GLTFNode : GLTFChildOfRootProperty
    {

        /// <summary>
        /// The index of the camera referenced by this node.
        /// </summary>
        public GLTFCameraId Camera;

        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        public List<GLTFNodeId> Children;

        /// <summary>
        /// The index of the skin referenced by this node.
        /// </summary>
        public GLTFSkinId Skin;

        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column-major order.
        /// </summary>
        public List<double> Matrix;

        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        public GLTFMeshId Mesh;

        /// <summary>
        /// The node's unit quaternion rotation in the order (x, y, z, w),
        /// where w is the scalar.
        /// </summary>
        public Quaternion Rotation = new Quaternion(0, 0, 0, 1);

        /// <summary>
        /// The node's non-uniform scale.
        /// </summary>
        public Vector3 Scale = Vector3.one;

        /// <summary>
        /// The node's translation.
        /// </summary>
        public Vector3 Translation = Vector3.zero;

        /// <summary>
        /// The weights of the instantiated Morph Target.
        /// Number of elements must match number of Morph Targets of used mesh.
        /// </summary>
        public List<double> Weights;

        public static GLTFNode Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var node = new GLTFNode();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "camera":
                        node.Camera = GLTFCameraId.Deserialize(root, reader);
                        break;
                    case "children":
                        node.Children = GLTFNodeId.ReadList(root, reader);
                        break;
                    case "skin":
                        node.Skin = GLTFSkinId.Deserialize(root, reader);
                        break;
                    case "matrix":
                        node.Matrix = reader.ReadDoubleList();
                        break;
                    case "mesh":
                        node.Mesh = GLTFMeshId.Deserialize(root, reader);
                        break;
                    case "rotation":
                        node.Rotation = reader.ReadAsQuaternion();
                        break;
                    case "scale":
                        node.Scale = reader.ReadAsVector3();
                        break;
                    case "translation":
                        node.Translation = reader.ReadAsVector3();
                        break;
                    case "weights":
                        node.Weights = reader.ReadDoubleList();
                        break;
					default:
						node.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return node;
        }
    }
}
