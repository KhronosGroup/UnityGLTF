using System.Collections.Generic;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// Joints and matrices defining a skin.
	/// </summary>
	public class Skin : GLTFChildOfRootProperty
	{
		/// <summary>
		/// The index of the accessor containing the floating-point 4x4 inverse-bind matrices.
		/// The default is that each matrix is a 4x4 Identity matrix, which implies that inverse-bind
		/// matrices were pre-applied.
		/// </summary>
		public AccessorId InverseBindMatrices;

		/// <summary>
		/// The index of the node used as a skeleton root.
		/// When undefined, joints transforms resolve to scene root.
		/// </summary>
		public NodeId Skeleton;

		/// <summary>
		/// Indices of skeleton nodes, used as joints in this skin.  The array length must be the
		// same as the `count` property of the `inverseBindMatrices` accessor (when defined).
		/// </summary>
		public List<NodeId> Joints;

		public Skin()
		{
			Joints = new List<NodeId>();
		}

		public static Skin Deserialize(GLTFRoot root, JsonReader reader)
		{
			var skin = new Skin();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "inverseBindMatrices":
						skin.InverseBindMatrices = AccessorId.Deserialize(root, reader);
						break;
					case "skeleton":
						skin.Skeleton = NodeId.Deserialize(root, reader);
						break;
					case "joints":
						skin.Joints = NodeId.ReadList(root, reader);
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
