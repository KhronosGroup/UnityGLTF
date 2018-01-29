using System.Collections.Generic;
using System.Linq;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// A node in the node hierarchy.
	/// When the node contains `skin`, all `mesh.primitives` must contain `JOINT`
	/// and `WEIGHT` attributes.  A node can have either a `matrix` or any combination
	/// of `translation`/`rotation`/`scale` (TRS) properties.
	/// TRS properties are converted to matrices and postmultiplied in
	/// the `T * R * S` order to compose the transformation matrix;
	/// first the scale is applied to the vertices, then the rotation, and then
	/// the translation. If none are provided, the transform is the Identity.
	/// When a node is targeted for animation
	/// (referenced by an animation.channel.target), only TRS properties may be present;
	/// `matrix` will not be present.
	/// </summary>
	public class Node : GLTFChildOfRootProperty
	{
		/// <summary>
		/// If true, extracts transform, rotation, scale values from the Matrix4x4. Otherwise uses the Transform, Rotate, Scale directly as specified by by the node.
		/// </summary>
		public bool UseTRS;

		/// <summary>
		/// The index of the camera referenced by this node.
		/// </summary>
		public CameraId Camera;

		/// <summary>
		/// The indices of this node's children.
		/// </summary>
		public List<NodeId> Children;

		/// <summary>
		/// The index of the skin referenced by this node.
		/// </summary>
		public SkinId Skin;

		/// <summary>
		/// A floating-point 4x4 transformation matrix stored in column-major order.
		/// </summary>
		public Matrix4x4 Matrix = Matrix4x4.Identity;

		/// <summary>
		/// The index of the mesh in this node.
		/// </summary>
		public MeshId Mesh;

		/// <summary>
		/// The node's unit quaternion rotation in the order (x, y, z, w),
		/// where w is the scalar.
		/// </summary>
		public Quaternion Rotation = new Quaternion(0, 0, 0, 1);

		/// <summary>
		/// The node's non-uniform scale.
		/// </summary>
		public Vector3 Scale = Vector3.One;

		/// <summary>
		/// The node's translation.
		/// </summary>
		public Vector3 Translation = Vector3.Zero;

		/// <summary>
		/// The weights of the instantiated Morph Target.
		/// Number of elements must match number of Morph Targets of used mesh.
		/// </summary>
		public List<double> Weights;

		public Node()
		{
		}

		public Node(Node node, GLTFRoot gltfRoot) : base(node, gltfRoot)
		{
			if (node == null) return;

			UseTRS = node.UseTRS;

			if (node.Camera != null)
			{
				Camera = new CameraId(node.Camera, gltfRoot);
			}

			if (node.Children != null)
			{
				Children = new List<NodeId>(node.Children.Count);
				foreach (NodeId child in node.Children)
				{
					Children.Add(new NodeId(child, gltfRoot));
				}
			}

			if (node.Skin != null)
			{
				Skin = new SkinId(node.Skin, gltfRoot);
			}

			if (node.Matrix != null)
			{
				Matrix = new Matrix4x4(node.Matrix);
			}

			if (node.Mesh != null)
			{
				Mesh = new MeshId(node.Mesh, gltfRoot);
			}

			Rotation = node.Rotation;

			Scale = node.Scale;

			Translation = node.Translation;

			if (node.Weights != null)
			{
				Weights = node.Weights.ToList();
			}
		}

		public static Node Deserialize(GLTFRoot root, JsonReader reader)
		{
			var node = new Node();

			while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
			{
				var curProp = reader.Value.ToString();

				switch (curProp)
				{
					case "camera":
						node.Camera = CameraId.Deserialize(root, reader);
						break;
					case "children":
						node.Children = NodeId.ReadList(root, reader);
						break;
					case "skin":
						node.Skin = SkinId.Deserialize(root, reader);
						break;
					case "matrix":
						var list = reader.ReadDoubleList();
						// gltf has column ordered matricies
						var mat = new Matrix4x4(
							(float)list[0],  (float)list[1],  (float)list[2],  (float)list[3], (float)list[4],  (float)list[5],  (float)list[6],  (float)list[7],
							(float)list[8],  (float)list[9],  (float)list[10], (float)list[11], (float)list[12], (float)list[13], (float)list[14], (float)list[15]
							);

						node.Matrix = mat;
						break;
					case "mesh":
						node.Mesh = MeshId.Deserialize(root, reader);
						break;
					case "rotation":
						node.UseTRS = true;
						node.Rotation = reader.ReadAsQuaternion();
						break;
					case "scale":
						node.UseTRS = true;
						node.Scale = reader.ReadAsVector3();
						break;
					case "translation":
						node.UseTRS = true;
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

		public override void Serialize(JsonWriter writer)
		{
			writer.WriteStartObject();

			if (Camera != null)
			{
				writer.WritePropertyName("camera");
				writer.WriteValue(Camera.Id);
			}

			if (Children != null && Children.Count > 0)
			{
				writer.WritePropertyName("children");
				writer.WriteStartArray();
				foreach (var child in Children)
				{
					writer.WriteValue(child.Id);
				}
				writer.WriteEndArray();
			}

			if (Skin != null)
			{
				writer.WritePropertyName("skin");
				writer.WriteValue(Skin.Id);
			}

			if (Matrix != Matrix4x4.Identity)
			{
				writer.WritePropertyName("matrix");
				writer.WriteStartArray();
				writer.WriteValue(Matrix.M11); writer.WriteValue(Matrix.M21); writer.WriteValue(Matrix.M31); writer.WriteValue(Matrix.M41);
				writer.WriteValue(Matrix.M12); writer.WriteValue(Matrix.M22); writer.WriteValue(Matrix.M32); writer.WriteValue(Matrix.M42);
				writer.WriteValue(Matrix.M13); writer.WriteValue(Matrix.M23); writer.WriteValue(Matrix.M33); writer.WriteValue(Matrix.M43);
				writer.WriteValue(Matrix.M14); writer.WriteValue(Matrix.M24); writer.WriteValue(Matrix.M34); writer.WriteValue(Matrix.M44);
				writer.WriteEndArray();
			}

			if (Mesh != null)
			{
				writer.WritePropertyName("mesh");
				writer.WriteValue(Mesh.Id);
			}

			if (Rotation != Quaternion.Identity)
			{
				writer.WritePropertyName("rotation");
				writer.WriteStartArray();
				writer.WriteValue(Rotation.X);
				writer.WriteValue(Rotation.Y);
				writer.WriteValue(Rotation.Z);
				writer.WriteValue(Rotation.W);
				writer.WriteEndArray();
			}

			if (Scale != Vector3.One)
			{
				writer.WritePropertyName("scale");
				writer.WriteStartArray();
				writer.WriteValue(Scale.X);
				writer.WriteValue(Scale.Y);
				writer.WriteValue(Scale.Z);
				writer.WriteEndArray();
			}

			if (Translation != Vector3.Zero)
			{
				writer.WritePropertyName("translation");
				writer.WriteStartArray();
				writer.WriteValue(Translation.X);
				writer.WriteValue(Translation.Y);
				writer.WriteValue(Translation.Z);
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

			base.Serialize(writer);

			writer.WriteEndObject();
		}
	}
}
