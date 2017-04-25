using System;
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
    public class GLTFMesh : GLTFChildOfRootProperty
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// <minItems>1</minItems>
        /// </summary>
        public List<GLTFMeshPrimitive> Primitives;

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// <minItems>0</minItems>
        /// </summary>
        public List<double> Weights;

        public static GLTFMesh Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var mesh = new GLTFMesh();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "primitives":
                        mesh.Primitives = reader.ReadList(() => GLTFMeshPrimitive.Deserialize(root, reader));
                        break;
                    case "weights":
                        mesh.Weights = reader.ReadDoubleList();
                        break;
					default:
						mesh.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return mesh;
        }

        public void BuildVertexAttributes()
        {
            foreach (var primitive in Primitives)
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
            var meshName = Name ?? "GLTFMesh";
            var meshObj = new GameObject(meshName);
            meshObj.transform.SetParent(parent.transform, false);
            // Flip the z scale to account for Unity's left handed coordinate system
            // vs GLTF's right handed system.
            var scale = meshObj.transform.localScale;
            scale.z *= -1;
            meshObj.transform.localScale = scale;

            if (Primitives.Count == 1)
            {
                Primitives[0].SetMeshAndMaterial(meshObj, meshName, config);
            }
            else
            {
                for (var i = 0; i < Primitives.Count; i++)
                {
                    var primitiveName = (Name ?? "GLTFMesh") + "_Primitive" + i;
                    var primitiveObj = new GameObject(primitiveName);
                    primitiveObj.transform.SetParent(meshObj.transform, false);
                    Primitives[0].SetMeshAndMaterial(primitiveObj, primitiveName, config);
                }
            }
        }
    }

    /// <summary>
    /// Geometry to be rendered with the given material.
    /// </summary>
    public class GLTFMeshPrimitive: GLTFProperty
    {
        /// <summary>
        /// A dictionary object, where each key corresponds to mesh attribute semantic
        /// and each value is the index of the accessor containing attribute's data.
        /// </summary>
        public Dictionary<string, GLTFAccessorId> Attributes = new Dictionary<string, GLTFAccessorId>();

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
        public GLTFAccessorId Indices;

        /// <summary>
        /// The index of the material to apply to this primitive when rendering.
        /// </summary>
        public GLTFMaterialId Material;

        /// <summary>
        /// The type of primitives to render. All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFDrawMode Mode = GLTFDrawMode.Triangles;

        /// <summary>
        /// An array of Morph Targets, each  Morph Target is a dictionary mapping
        /// attributes (only "POSITION" and "NORMAL" supported) to their deviations
        /// in the Morph Target (index of the accessor containing the attribute
        /// displacements' data).
        /// </summary>
        /// TODO: Make dictionary key enums?
        public List<Dictionary<string, GLTFAccessorId>> Targets;

        public static GLTFMeshPrimitive Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var primitive = new GLTFMeshPrimitive();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "attributes":
                        primitive.Attributes = reader.ReadAsDictionary(() => new GLTFAccessorId
                        {
                            Id = reader.ReadAsInt32().Value,
                            Root = root
                        });
                        break;
                    case "indices":
                        primitive.Indices = GLTFAccessorId.Deserialize(root, reader);
                        break;
                    case "material":
                        primitive.Material = GLTFMaterialId.Deserialize(root, reader);
                        break;
                    case "mode":
                        primitive.Mode = (GLTFDrawMode) reader.ReadAsInt32().Value;
                        break;
                    case "targets":
                        primitive.Targets = reader.ReadList(() =>
                        {
                            return reader.ReadAsDictionary(() => new GLTFAccessorId
                            {
                                Id = reader.ReadAsInt32().Value,
                                Root = root
                            });
                        });
                        break;
					default:
						primitive.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return primitive;
        }

        // Stored reference to the Mesh so we don't have to regenerate it if used in multiple nodes.
        private Mesh _mesh;

        private Vector3[] _vertices;
        private Vector3[] _normals;
        private Vector2[] _uv;
        private Vector2[] _uv2;
        private Vector2[] _uv3;
        private Vector2[] _uv4;
        private Color[] _colors;
        private int[] _triangles;
        private Vector4[] _tangents;

        public void BuildVertexAttributes()
        {
            if (Attributes.ContainsKey(GLTFSemanticProperties.POSITION))
            {
                _vertices = Attributes[GLTFSemanticProperties.POSITION].Value.AsVector3Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.NORMAL))
            {
                _normals = Attributes[GLTFSemanticProperties.NORMAL].Value.AsVector3Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(0)))
            {
                _uv = Attributes[GLTFSemanticProperties.TexCoord(0)].Value.AsVector2Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(1)))
            {
                _uv2 = Attributes[GLTFSemanticProperties.TexCoord(1)].Value.AsVector2Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(2)))
            {
	            _uv3 = Attributes[GLTFSemanticProperties.TexCoord(2)].Value.AsVector2Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(3)))
            {
	            _uv4 = Attributes[GLTFSemanticProperties.TexCoord(3)].Value.AsVector2Array();
            }
            if (Attributes.ContainsKey(GLTFSemanticProperties.Color(0)))
            {
	            _colors = Attributes[GLTFSemanticProperties.Color(0)].Value.AsColorArray();
            }

	        _triangles = Indices.Value.AsIntArray();

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
            var meshFilter = primitiveObj.AddComponent<MeshFilter>();

            if (_mesh == null)
            {
	            _mesh = new Mesh
	            {
		            name = meshName,
		            vertices = _vertices,
		            normals = _normals,
		            uv = _uv,
		            uv2 = _uv2,
		            uv3 = _uv3,
		            uv4 = _uv4,
		            colors = _colors,
		            triangles = _triangles,
		            tangents = _tangents
	            };
            }

            meshFilter.mesh = _mesh;

            var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();

			meshRenderer.material = Material.Value.GetMaterial(config);
        }


        // Taken from: http://answers.unity3d.com/comments/190515/view.html
        // Official support for Mesh.RecalculateTangents should be coming in 5.6
        // https://feedback.unity3d.com/suggestions/recalculatetangents
        private void CalculateMeshTangents()
        {
            var triangleCount = _triangles.Length;
            var vertexCount = _vertices.Length;

            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            _tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = _triangles[a + 0];
                long i2 = _triangles[a + 1];
                long i3 = _triangles[a + 2];

                var v1 = _vertices[i1];
	            var v2 = _vertices[i2];
	            var v3 = _vertices[i3];

	            var w1 = _uv[i1];
	            var w2 = _uv[i2];
	            var w3 = _uv[i3];

	            var x1 = v2.x - v1.x;
	            var x2 = v3.x - v1.x;
	            var y1 = v2.y - v1.y;
	            var y2 = v3.y - v1.y;
	            var z1 = v2.z - v1.z;
	            var z2 = v3.z - v1.z;

	            var s1 = w2.x - w1.x;
	            var s2 = w3.x - w1.x;
	            var t1 = w2.y - w1.y;
	            var t2 = w3.y - w1.y;

	            var r = 1.0f / (s1 * t2 - s2 * t1);

	            var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
	            var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (long a = 0; a < vertexCount; ++a)
            {
	            var n = _normals[a];
	            var t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);

                _tangents[a].x = t.x;
	            _tangents[a].y = t.y;
	            _tangents[a].z = t.z;

	            _tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
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
	        var parts = property.Split('_');

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
        Points = 0,
        Lines = 1,
        LineLoop = 2,
        LineStrip = 3,
        Triangles = 4,
        TriangleStrip = 5,
        TriangleFan = 6
    }
}
