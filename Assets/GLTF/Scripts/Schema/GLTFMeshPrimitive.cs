using System.Collections.Generic;
using Newtonsoft.Json;
using GLTF.JsonExtensions;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// Geometry to be rendered with the given material.
    /// </summary>
    public class GLTFMeshPrimitive : GLTFProperty
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
                        primitive.Mode = (GLTFDrawMode)reader.ReadAsInt32().Value;
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
    }

    public struct GLTFMeshPrimitiveAttributes
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uv;
        public Vector2[] Uv2;
        public Vector2[] Uv3;
        public Vector2[] Uv4;
        public Color[] Colors;
        public int[] Triangles;
        public Vector4[] Tangents;
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
