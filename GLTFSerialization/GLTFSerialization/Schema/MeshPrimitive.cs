using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// Geometry to be rendered with the given material.
	/// </summary>
	public class MeshPrimitive : GLTFProperty
	{
		/// <summary>
		/// A dictionary object, where each key corresponds to mesh attribute semantic
		/// and each value is the index of the accessor containing attribute's data.
		/// </summary>
		public Dictionary<string, AccessorId> Attributes = new Dictionary<string, AccessorId>();

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
		public AccessorId Indices;

		/// <summary>
		/// The index of the material to apply to this primitive when rendering.
		/// </summary>
		public MaterialId Material;

		/// <summary>
		/// The type of primitives to render. All valid values correspond to WebGL enums.
		/// </summary>
		public DrawMode Mode = DrawMode.Triangles;

		/// <summary>
		/// An array of Morph Targets, each  Morph Target is a dictionary mapping
		/// attributes (only "POSITION" and "NORMAL" supported) to their deviations
		/// in the Morph Target (index of the accessor containing the attribute
		/// displacements' data).
		/// </summary>
		/// TODO: Make dictionary key enums?
		public List<Dictionary<string, AccessorId>> Targets;

		public static int[] GenerateTriangles(int vertCount)
		{
			var arr = new int[vertCount];
			for (var i = 0; i < vertCount; i+=3)
			{
				arr[i] = i + 2;
				arr[i + 1] = i + 1;
				arr[i + 2] = i;
			}

			return arr;
		}

		public MeshPrimitive Clone()
		{
			return new MeshPrimitive
			{
				Attributes = Attributes,
				Indices = Indices,
				Material = Material,
				Mode = Mode,
				Targets = Targets
			};
		}

		// Taken from: http://answers.unity3d.com/comments/190515/view.html
		// Official support for Mesh.RecalculateTangents should be coming in 5.6
		// https://feedback.unity3d.com/suggestions/recalculatetangents
		/*private MeshPrimitiveAttributes CalculateAndSetTangents(MeshPrimitiveAttributes attributes)
		{
			var triangleCount = attributes.Triangles.Length;
			var vertexCount = attributes.Vertices.Length;

			var tan1 = new Vector3[vertexCount];
			var tan2 = new Vector3[vertexCount];

			attributes.Tangents = new Vector4[vertexCount];

			for (long a = 0; a < triangleCount; a += 3)
			{
				long i1 = attributes.Triangles[a + 0];
				long i2 = attributes.Triangles[a + 1];
				long i3 = attributes.Triangles[a + 2];

				var v1 = attributes.Vertices[i1];
				var v2 = attributes.Vertices[i2];
				var v3 = attributes.Vertices[i3];

				var w1 = attributes.Uv[i1];
				var w2 = attributes.Uv[i2];
				var w3 = attributes.Uv[i3];

				var x1 = v2.X - v1.X;
				var x2 = v3.X - v1.X;
				var y1 = v2.Y - v1.Y;
				var y2 = v3.Y - v1.Y;
				var z1 = v2.Z - v1.Z;
				var z2 = v3.Z - v1.Z;

				var s1 = w2.X - w1.X;
				var s2 = w3.X - w1.X;
				var t1 = w2.Y - w1.Y;
				var t2 = w3.Y - w1.Y;

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
				var n = attributes.Normals[a];
				var t = tan1[a];

				Vector3.OrthoNormalize(ref n, ref t);

				attributes.Tangents[a].X = t.X;
				attributes.Tangents[a].Y = t.Y;
				attributes.Tangents[a].Z = t.Z;

				attributes.Tangents[a].W = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			}

			return attributes;
		}*/

		public static MeshPrimitive Deserialize(GLTFRoot root, JsonReader reader)
		{
			var primitive = new MeshPrimitive();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "attributes":
						primitive.Attributes = reader.ReadAsDictionary(() => new AccessorId
						{
							Id = reader.ReadAsInt32().Value,
							Root = root
						});
						break;
					case "indices":
						primitive.Indices = AccessorId.Deserialize(root, reader);
						break;
					case "material":
						primitive.Material = MaterialId.Deserialize(root, reader);
						break;
					case "mode":
						primitive.Mode = (DrawMode)reader.ReadAsInt32().Value;
						break;
					case "targets":
						primitive.Targets = reader.ReadList(() => reader.ReadAsObject(() => AccessorId.Deserialize(root, reader)));

						break;
					default:
						primitive.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return primitive;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			writer.WritePropertyName("attributes");
			writer.WriteStartObject();
			foreach (var attribute in Attributes)
			{
				writer.WritePropertyName(attribute.Key);
				writer.WriteValue(attribute.Value.Id);
			}
			writer.WriteEndObject();

			if (Indices != null)
			{
				writer.WritePropertyName("indices");
				writer.WriteValue(Indices.Id);
			}

			if (Material != null)
			{
				writer.WritePropertyName("material");
				writer.WriteValue(Material.Id);
			}

			if (Mode != DrawMode.Triangles)
			{
				writer.WritePropertyName("mode");
				writer.WriteValue((int)Mode);
			}

			if (Targets != null && Targets.Count > 0)
			{
				writer.WritePropertyName("targets");
				writer.WriteStartArray();
				foreach (var target in Targets)
				{
					writer.WriteStartObject();

					foreach (var attribute in target) {
						writer.WritePropertyName(attribute.Key);
						writer.WriteValue(attribute.Value.Id);
					}

					writer.WriteEndObject();
				}
				writer.WriteEndArray();
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}

	public static class SemanticProperties
	{
		public static readonly string POSITION = "POSITION";
		public static readonly string NORMAL = "NORMAL";
		public static readonly string JOINT = "JOINTS_0";
		public static readonly string WEIGHT = "WEIGHTS_0";
		public static readonly string TANGENT = "TANGENT";
		public static readonly string INDICES = "INDICIES";

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

	public enum DrawMode
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
