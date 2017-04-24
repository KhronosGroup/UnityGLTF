using UnityEngine;
using System;
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
    [Serializable]
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
                    case "name":
                        node.Name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return node;
        }

        /// <summary>
        /// Create the GameObject for the GLTFNode and set it as a child of the parent GameObject.
        /// </summary>
        /// <param name="parent">This node's parent GameObject</param>
        /// <param name="config">Config for GLTF scene creation.</param>
        public void Create(GameObject parent, GLTFConfig config)
        {
            var nodeObj = new GameObject(Name ?? "GLTFNode");
            nodeObj.transform.parent = parent.transform;

            // Set the transform properties from the GLTFNode's values.
            // Use the matrix first if set.
            if (Matrix != null)
            {
                var mat = new Matrix4x4();

                for (var i = 0; i < 16; i++)
                {
                    mat[i] = (float)Matrix[i];
                }

                nodeObj.transform.localPosition = mat.GetColumn(3);

                nodeObj.transform.localScale = new Vector3(
                    mat.GetColumn(0).magnitude,
                    mat.GetColumn(1).magnitude,
                    mat.GetColumn(2).magnitude
                );

                var w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
	            var w4 = 4.0f * w;
	            var x = (mat.m21 - mat.m12) / w4;
	            var y = (mat.m02 - mat.m20) / w4;
	            var z = (mat.m10 - mat.m01) / w4;

                x = float.IsNaN(x) ? 0 : x;
                y = float.IsNaN(y) ? 0 : y;
                z = float.IsNaN(z) ? 0 : z;

                nodeObj.transform.localRotation = new Quaternion(x, y, z, w);
            }
            // Otherwise fall back to the TRS properties.
            else
            {
                nodeObj.transform.localPosition = Translation;
                nodeObj.transform.localScale = Scale;
                nodeObj.transform.localRotation = Rotation;
            }

            // TODO: Add support for skin/morph targets
            if (Mesh != null)
            {
                Mesh.Value.SetMeshesAndMaterials(nodeObj, config);
            }

            /* TODO: implement camera (probably a flag to disable for VR as well)
            if (camera != null)
            {
                GameObject cameraObj = camera.Value.Create();
                cameraObj.transform.parent = nodeObj.transform;
            }
            */

            foreach(var child in Children)
            {
                child.Value.Create(nodeObj, config);
            }
        }
    }
}
