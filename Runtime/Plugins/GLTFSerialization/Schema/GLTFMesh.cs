using System.Collections.Generic;
using System.Linq;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
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
		public List<MeshPrimitive> Primitives;

		/// <summary>
		/// Array of weights to be applied to the Morph Targets.
		/// <minItems>0</minItems>
		/// </summary>
		public List<double> Weights;

		public List<string> TargetNames;

		public GLTFMesh()
		{
		}

		public GLTFMesh(GLTFMesh mesh, GLTFRoot gltfRoot) : base(mesh, gltfRoot)
		{
			if (mesh == null) return;

			if (mesh.Primitives != null)
			{
				Primitives = new List<MeshPrimitive>(mesh.Primitives.Count);

				foreach (MeshPrimitive primitive in mesh.Primitives)
				{
					Primitives.Add(new MeshPrimitive(primitive, gltfRoot));
				}
			}

			if (mesh.Weights != null)
			{
				Weights = mesh.Weights.ToList();
			}
		}


		public static GLTFMesh Deserialize(GLTFRoot root, JsonReader reader)
		{
			var mesh = new GLTFMesh();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "primitives":
						mesh.Primitives = reader.ReadList(() => MeshPrimitive.Deserialize(root, reader));
						break;
					case "weights":
						mesh.Weights = reader.ReadDoubleList();
						break;
					default:
						mesh.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}
			
			// GLTF does not support morph target names, serialize in extras for now
			// https://github.com/KhronosGroup/glTF/issues/1036
			if (mesh.Extras != null)
			{
				var extrasReader = mesh.Extras.CreateReader();
				extrasReader.Read();

				while (extrasReader.Read() && extrasReader.TokenType == JsonToken.PropertyName)
				{
					var extraProperty = extrasReader.Value.ToString();
					switch (extraProperty)
					{
						case "targetNames":
							mesh.TargetNames = extrasReader.ReadStringList();
							break;
						default:
							extrasReader.Skip();
							break;
					}
				}

				extrasReader.Close();
			}

			return mesh;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Primitives != null && Primitives.Count > 0)
			{
				writer.WritePropertyName("primitives");
				writer.WriteStartArray();
				foreach (var primitive in Primitives)
				{
					primitive.Serialize(writer);
				}
				writer.WriteEndArray();
			}

			if (Weights != null && Weights.Count > 0)
			{
				writer.WritePropertyName("weights");
				writer.WriteStartArray();
				foreach (var weight in Weights)
				{
					writer.WriteValue(weight);
				}
				writer.WriteEndArray();
			}
			
			// GLTF does not support morph target names, serialize in extras for now
			// https://github.com/KhronosGroup/glTF/issues/1036
			if (TargetNames != null && TargetNames.Count > 0)
			{
				writer.WritePropertyName("extras");
				writer.WriteStartObject();
				writer.WritePropertyName("targetNames");
				writer.WriteStartArray();
				foreach (var targetName in TargetNames)
				{
					writer.WriteValue(targetName);
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
