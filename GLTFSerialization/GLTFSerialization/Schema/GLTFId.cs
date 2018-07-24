using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{

	/// <summary>
	/// Abstract class that stores a reference to the root GLTF object and an id
	/// of an object of type T inside it.
	/// </summary>
	/// <typeparam name="T">The value type returned by the GLTFId reference.</typeparam>
	public abstract class GLTFId<T>
	{
		public int Id;
		public GLTFRoot Root;
		public abstract T Value { get; }

		protected GLTFId()
		{
		}

		public GLTFId(GLTFId<T> gltfId, GLTFRoot newRoot)
		{
			Id = gltfId.Id;
			Root = newRoot;
		}

		public void Serialize(JsonWriter writer)
		{
			writer.WriteValue(Id);
		}
	}

	public class AccessorId : GLTFId<Accessor>
	{
		public AccessorId()
		{
		}

		public AccessorId(AccessorId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override Accessor Value
		{
			get { return Root.Accessors[Id]; }
		}

		public static AccessorId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new AccessorId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class BufferId : GLTFId<GLTFBuffer>
	{
		public BufferId()
		{
		}

		public BufferId(BufferId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFBuffer Value
		{
			get { return Root.Buffers[Id]; }
		}

		public static BufferId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new BufferId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class BufferViewId : GLTFId<BufferView>
	{
		public BufferViewId()
		{
		}

		public BufferViewId(BufferViewId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override BufferView Value
		{
			get { return Root.BufferViews[Id]; }
		}

		public static BufferViewId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new BufferViewId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class CameraId : GLTFId<GLTFCamera>
	{
		public CameraId()
		{
		}

		public CameraId(CameraId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFCamera Value
		{
			get { return Root.Cameras[Id]; }
		}

		public static CameraId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new CameraId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class ImageId : GLTFId<GLTFImage>
	{
		public ImageId()
		{
		}

		public ImageId(ImageId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}


		public override GLTFImage Value
		{
			get { return Root.Images[Id]; }
		}

		public static ImageId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new ImageId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class MaterialId : GLTFId<GLTFMaterial>
	{
		public MaterialId()
		{
		}

		public MaterialId(MaterialId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFMaterial Value
		{
			get { return Root.Materials[Id]; }
		}

		public static MaterialId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new MaterialId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class MeshId : GLTFId<GLTFMesh>
	{
		public MeshId()
		{
		}

		public MeshId(MeshId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFMesh Value
		{
			get { return Root.Meshes[Id]; }
		}

		public static MeshId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new MeshId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class NodeId : GLTFId<Node>
	{
		public NodeId()
		{
		}

		public NodeId(NodeId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override Node Value
		{
			get { return Root.Nodes[Id]; }
		}

		public static NodeId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new NodeId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}

		public static List<NodeId> ReadList(GLTFRoot root, JsonReader reader)
		{
			if (reader.Read() && reader.TokenType != JsonToken.StartArray)
			{
				throw new Exception("Invalid array.");
			}

			var list = new List<NodeId>();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				var node = new NodeId
				{
					Id = int.Parse(reader.Value.ToString()),
					Root = root
				};

				list.Add(node);
			}

			return list;
		}
	}

	public class SamplerId : GLTFId<Sampler>
	{
		public SamplerId()
		{
		}

		public SamplerId(SamplerId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override Sampler Value
		{
			get { return Root.Samplers[Id]; }
		}

		public static SamplerId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new SamplerId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class SceneId : GLTFId<GLTFScene>
	{
		public SceneId()
		{
		}

		public SceneId(SceneId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}


		public override GLTFScene Value
		{
			get { return Root.Scenes[Id]; }
		}

		public static SceneId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new SceneId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class SkinId : GLTFId<Skin>
	{
		public SkinId()
		{
		}

		public SkinId(SkinId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override Skin Value
		{
			get { return Root.Skins[Id]; }
		}

		public static SkinId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new SkinId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
	}

	public class TextureId : GLTFId<GLTFTexture>
	{
		public TextureId()
		{
		}

		public TextureId(TextureId id, GLTFRoot newRoot) : base(id, newRoot)
		{
		}

		public override GLTFTexture Value
		{
			get { return Root.Textures[Id]; }
		}

		public static TextureId Deserialize(GLTFRoot root, JsonReader reader)
		{
			return new TextureId
			{
				Id = reader.ReadAsInt32().Value,
				Root = root
			};
		}
		public static TextureId Deserialize(GLTFRoot root, JProperty jProperty)
		{
			return new TextureId
			{
				Id = (int)jProperty.Value,
				Root = root
			};
		}
	}
}
