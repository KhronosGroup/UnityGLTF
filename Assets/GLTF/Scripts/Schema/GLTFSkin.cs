using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
	/// <summary>
	/// Joints and matrices defining a skin.
	/// </summary>
	public class GLTFSkin : GLTFChildOfRootProperty
	{
		/// <summary>
		/// The index of the accessor containing the floating-point 4x4 inverse-bind matrices.
		/// The default is that each matrix is a 4x4 identity matrix, which implies that inverse-bind
		/// matrices were pre-applied.
		/// </summary>
		public GLTFAccessorId InverseBindMatrices;

		/// <summary>
		/// The index of the node used as a skeleton root.
		/// When undefined, joints transforms resolve to scene root.
		/// </summary>
		public GLTFNodeId Skeleton;

		/// <summary>
		/// Indices of skeleton nodes, used as joints in this skin.  The array length must be the
		// same as the `count` property of the `inverseBindMatrices` accessor (when defined).
		/// </summary>
		public List<GLTFNodeId> Joints;

		public static GLTFSkin Deserialize(GLTFRoot root, JsonReader reader)
		{
			var skin = new GLTFSkin();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "inverseBindMatrices":
						skin.InverseBindMatrices = GLTFAccessorId.Deserialize(root, reader);
						break;
					case "skeleton":
						skin.Skeleton = GLTFNodeId.Deserialize(root, reader);
						break;
					case "joints":
						skin.Joints = reader.ReadList(() => GLTFNodeId.Deserialize(root, reader));
						break;
					default:
						skin.DefaultPropertyDeserializer(root, reader);
						break;
				}
			}

			return skin;
		}

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (InverseBindMatrices != null)
			{
				writer.WritePropertyName("inverseBindMatrices");
				writer.WriteValue(InverseBindMatrices.Id);
			}

			if (Skeleton != null)
			{
				writer.WritePropertyName("skeleton");
				writer.WriteValue(Skeleton.Id);
			}

			if (Joints != null && Joints.Count > 0)
			{
				writer.WritePropertyName("joints");
				writer.WriteStartArray();
				foreach (var joint in Joints)
				{
					writer.WriteValue(joint.Id);
				}
				writer.WriteEndArray();
			}

			base.Serialize(writer);
			
			writer.WriteEndObject();
		}
	}
}
