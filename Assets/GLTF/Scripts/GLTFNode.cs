using UnityEngine;

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
    public class GLTFNode
    {
        /// <summary>
        /// The index of the camera referenced by this node.
        /// </summary>
        public GLTFCameraId camera;

        /// <summary>
        /// The indices of this node's children.
        /// </summary>
        public GLTFNodeId[] children = { };

        /// <summary>
        /// The index of the skin referenced by this node.
        /// </summary>
        public GLTFSkinId skin;

        /// <summary>
        /// A floating-point 4x4 transformation matrix stored in column-major order.
        /// </summary>
        public double[] matrix;

        /// <summary>
        /// The index of the mesh in this node.
        /// </summary>
        public GLTFMeshId mesh;

        /// <summary>
        /// The node's unit quaternion rotation in the order (x, y, z, w),
        /// where w is the scalar.
        /// </summary>
        public double[] rotation = { 0, 0, 0, 1 };

        /// <summary>
        /// The node's non-uniform scale.
        /// </summary>
        public double[] scale = { 1, 1, 1 };

        /// <summary>
        /// The node's translation.
        /// </summary>
        public double[] translation = { 0, 0, 0 };

        /// <summary>
        /// The weights of the instantiated Morph Target.
        /// Number of elements must match number of Morph Targets of used mesh.
        /// </summary>
        public double[] weights;

        public string name;

        /// <summary>
        /// Create the GameObject for the GLTFNode and set it as a child of the parent GameObject.
        /// </summary>
        /// <param name="parent">This node's parent GameObject</param>
        public void Create(GameObject parent)
        {
            GameObject nodeObj = new GameObject(name ?? "GLTFNode");
            nodeObj.transform.parent = parent.transform;

            // Set the transform properties from the GLTFNode's values.
            // Use the matrix first if set.
            if (matrix != null)
            {
                Matrix4x4 mat = new Matrix4x4();

                for (int i = 0; i < 16; i++)
                {
                    mat[i] = (float)matrix[i];
                }

                nodeObj.transform.localPosition = mat.GetColumn(3);

                nodeObj.transform.localScale = new Vector3(
                    mat.GetColumn(0).magnitude,
                    mat.GetColumn(1).magnitude,
                    mat.GetColumn(2).magnitude
                );

                float w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
                float w4 = 4.0f * w;
                float x = (mat.m21 - mat.m12) / w4;
                float y = (mat.m02 - mat.m20) / w4;
                float z = (mat.m10 - mat.m01) / w4;

                x = float.IsNaN(x) ? 0 : x;
                y = float.IsNaN(y) ? 0 : y;
                z = float.IsNaN(z) ? 0 : z;

                nodeObj.transform.localRotation = new Quaternion(x, y, z, w);
            }
            // Otherwise fall back to the TRS properties.
            else
            {
                nodeObj.transform.localPosition = new Vector3(
                    (float)translation[0],
                    (float)translation[1],
                    (float)translation[2]
                );
                nodeObj.transform.localScale = new Vector3(
                    (float)scale[0],
                    (float)scale[1],
                    (float)scale[2]
                );
                nodeObj.transform.localRotation = new Quaternion(
                    (float)rotation[0],
                    (float)rotation[1],
                    (float)rotation[2],
                    (float)rotation[3]
                );
            }

            // TODO: Add support for skin/morph targets
            if (mesh != null)
            {
                mesh.Value.SetMeshesAndMaterials(nodeObj);
            }

            /* TODO: implement camera (probably a flag to disable for VR as well)
            if (camera != null)
            {
                GameObject cameraObj = camera.Value.Create();
                cameraObj.transform.parent = nodeObj.transform;
            }
            */

            foreach(var child in children)
            {
                child.Value.Create(nodeObj);
            }
        }
    }
}
