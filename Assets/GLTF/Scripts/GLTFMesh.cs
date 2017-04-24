using System.Collections.Generic;
using Newtonsoft.Json;
using GLTF.JsonExtensions;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// A set of primitives to be rendered. A node can contain one or more meshes.
    /// A node's transform places the mesh in the scene.
    /// </summary>
    [System.Serializable]
    public class GLTFMesh
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// <minItems>1</minItems>
        /// </summary>
        public List<GLTFMeshPrimitive> primitives;

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// <minItems>0</minItems>
        /// </summary>
        public List<double> weights;

        public string name;

        public static GLTFMesh Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var mesh = new GLTFMesh();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "primitives":
                        mesh.primitives = reader.ReadList(() => GLTFMeshPrimitive.Deserialize(root, reader));
                        break;
                    case "weights":
                        mesh.weights = reader.ReadDoubleList();
                        break;
                    case "name":
                        mesh.name = reader.ReadAsString();
                        break;
                    case "extensions":
                        break;
                    case "extras":
                        break;
                }
            }

            return mesh;
        }

        public void BuildVertexAttributes()
        {
            foreach (var primitive in primitives)
            {
                primitive.BuildVertexAttributes();
            }
        }

        /// <summary>
        /// Build the meshes and materials for the GLTFMesh and attach them to the parent object.
        /// </summary>
        /// <param name="parent">GameObject of the parent GLTFNode</param>
        /// <param name="config">Config for GLTF scene creation.</param>
        public void SetMeshesAndMaterials(GameObject parent, GLTFConfig config)
        {
            string meshName = name ?? "GLTFMesh";
            GameObject meshObj = new GameObject(meshName);
            meshObj.transform.SetParent(parent.transform, false);
            // Flip the z scale to account for Unity's left handed coordinate system
            // vs GLTF's right handed system.
            Vector3 scale = meshObj.transform.localScale;
            scale.z *= -1;
            meshObj.transform.localScale = scale;

            if (primitives.Count == 1)
            {
                primitives[0].SetMeshAndMaterial(meshObj, meshName, config);
            }
            else
            {
                for (int i = 0; i < primitives.Count; i++)
                {
                    string primitiveName = (name ?? "GLTFMesh") + "_Primitive" + i;
                    GameObject primitiveObj = new GameObject(primitiveName);
                    primitiveObj.transform.SetParent(meshObj.transform, false);
                    primitives[0].SetMeshAndMaterial(primitiveObj, primitiveName, config);
                }
            }
        }
    }

    /// <summary>
    /// Geometry to be rendered with the given material.
    /// </summary>
    [System.Serializable]
    public class GLTFMeshPrimitive
    {
        /// <summary>
        /// A dictionary object, where each key corresponds to mesh attribute semantic
        /// and each value is the index of the accessor containing attribute's data.
        /// </summary>
        public Dictionary<string, GLTFAccessorId> attributes = new Dictionary<string, GLTFAccessorId>();

        /// <summary>
        /// The index of the accessor that contains mesh indices.
        /// When this is not defined, the primitives should be rendered without indices
        /// using `drawArrays()`. When defined, the accessor must contain indices:
        /// the `bufferView` referenced by the accessor must have a `target` equal
        /// to 34963 (ELEMENT_ARRAY_BUFFER); a `byteStride` that is tightly packed,
        /// i.e., 0 or the byte size of `componentType` in bytes;
        /// `componentType` must be 5121 (UNSIGNED_BYTE), 5123 (UNSIGNED_SHORT)
        /// or 5125 (UNSIGNED_INT), the latter is only allowed
        /// when `OES_element_index_uint` extension is used; `type` must be `\"SCALAR\"`.
        /// </summary>
        public GLTFAccessorId indices;

        /// <summary>
        /// The index of the material to apply to this primitive when rendering.
        /// </summary>
        public GLTFMaterialId material;

        /// <summary>
        /// The type of primitives to render. All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFDrawMode mode = GLTFDrawMode.TRIANGLES;

        /// <summary>
        /// An array of Morph Targets, each  Morph Target is a dictionary mapping
        /// attributes (only "POSITION" and "NORMAL" supported) to their deviations
        /// in the Morph Target (index of the accessor containing the attribute
        /// displacements' data).
        /// </summary>
        /// TODO: Make dictionary key enums?
        public List<Dictionary<string, GLTFAccessorId>> targets;

        public static GLTFMeshPrimitive Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var primitive = new GLTFMeshPrimitive();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "attributes":
                        primitive.attributes = reader.ReadAsDictionary(() => new GLTFAccessorId
                        {
                            id = reader.ReadAsInt32().Value,
                            root = root
                        });
                        break;
                    case "indices":
                        primitive.indices = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "material":
                        primitive.material = GLTFMaterialId.Deserialize(root, reader);
                        break;
                    case "mode":
                        primitive.mode = (GLTFDrawMode) reader.ReadAsInt32().Value;
                        break;
                    case "targets":
                        primitive.targets = reader.ReadList(() =>
                        {
                            return reader.ReadAsDictionary(() => new GLTFAccessorId
                            {
                                id = reader.ReadAsInt32().Value,
                                root = root
                            });
                        });
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return primitive;
        }

        // Stored reference to the Mesh so we don't have to regenerate it if used in multiple nodes.
        private Mesh mesh;

        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector2[] uv;
        private Vector2[] uv2;
        private Vector2[] uv3;
        private Vector2[] uv4;
        private Color[] colors;
        private int[] triangles;
        private Vector4[] tangents;

        public void BuildVertexAttributes()
        {
            if (attributes.ContainsKey(GLTFSemanticProperties.POSITION))
            {
                vertices = attributes[GLTFSemanticProperties.POSITION].Value.AsVector3Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.NORMAL))
            {
                normals = attributes[GLTFSemanticProperties.NORMAL].Value.AsVector3Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.TexCoord(0)))
            {
                uv = attributes[GLTFSemanticProperties.TexCoord(0)].Value.AsVector2Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.TexCoord(1)))
            {
                uv2 = attributes[GLTFSemanticProperties.TexCoord(1)].Value.AsVector2Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.TexCoord(2)))
            {
                uv3 = attributes[GLTFSemanticProperties.TexCoord(2)].Value.AsVector2Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.TexCoord(3)))
            {
                uv4 = attributes[GLTFSemanticProperties.TexCoord(3)].Value.AsVector2Array();
            }
            if (attributes.ContainsKey(GLTFSemanticProperties.Color(0)))
            {
                colors = attributes[GLTFSemanticProperties.Color(0)].Value.AsColorArray();
            }

            triangles = indices.Value.AsIntArray();

            CalculateMeshTangents();
        }

        /// <summary>
        /// Build the mesh and material for the GLTFPrimitive and attach them to the primitive object.
        /// </summary>
        /// <param name="parent">GameObject of the parent GLTFNode</param>
        /// <param name="meshName">The name to be assigned to the Mesh object</param>
        /// <param name="config">Config for GLTF scene creation.</param>
        public void SetMeshAndMaterial(GameObject primitiveObj, string meshName, GLTFConfig config)
        {
            MeshFilter meshFilter = primitiveObj.AddComponent<MeshFilter>();

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = meshName;
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uv;
                mesh.uv2 = uv2;
                mesh.uv3 = uv3;
                mesh.uv4 = uv4;
                mesh.colors = colors;
                mesh.triangles = triangles;
                mesh.tangents = tangents;
            }

            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
            
            meshRenderer.material = material.Value.GetMaterial(config);
        }


        // Taken from: http://answers.unity3d.com/comments/190515/view.html
        // Official support for Mesh.RecalculateTangents should be coming in 5.6
        // https://feedback.unity3d.com/suggestions/recalculatetangents
        private void CalculateMeshTangents()
        {
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = triangles[a + 0];
                long i2 = triangles[a + 1];
                long i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (long a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);

                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }
        }
    }

    public static class GLTFSemanticProperties
    {
        public static readonly string POSITION = "POSITION";
        public static readonly string NORMAL = "NORMAL";
        public static readonly string JOINT = "JOINT";
        public static readonly string WEIGHT = "WEIGHT";

        /// <summary>
        /// Return the semantic property for the uv buffer.
        /// </summary>
        /// <param name="index">The index of the uv buffer</param>
        /// <returns>The semantic property for the uv buffer</returns>
        public static string TexCoord(int index)
        {
            return "TEXCOORD_" + index;
        }

        /// <summary>
        /// Return the semantic property for the color buffer.
        /// </summary>
        /// <param name="index">The index of the color buffer</param>
        /// <returns>The semantic property for the color buffer</returns>
        public static string Color(int index)
        {
            return "COLOR_" + index;
        }

        /// <summary>
        /// Parse out the index of a given semantic property.
        /// </summary>
        /// <param name="property">Semantic property to parse</param>
        /// <param name="index">Parsed index to assign</param>
        /// <returns></returns>
        public static bool ParsePropertyIndex(string property, out int index)
        {
            index = -1;
            string[] parts = property.Split('_');

            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out index))
            {
                return false;
            }

            return true;
        }
    }

    public enum GLTFDrawMode
    {
        POINTS = 0,
        LINES = 1,
        LINE_LOOP = 2,
        LINE_STRIP = 3,
        TRIANGLES = 4,
        TRIANGLE_STRIP = 5,
        TRIANGLE_FAN = 6
    }
}
