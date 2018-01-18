using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Extensions;
using Newtonsoft.Json;

namespace GLTF.Schema
{
	/// <summary>
	/// The root object for a glTF asset.
	/// </summary>
	public class GLTFRoot : GLTFProperty
	{
		/// <summary>
		/// Names of glTF extensions used somewhere in this asset.
		/// </summary>
		public List<string> ExtensionsUsed;

		/// <summary>
		/// Names of glTF extensions required to properly load this asset.
		/// </summary>
		public List<string> ExtensionsRequired;

		/// <summary>
		/// An array of accessors. An accessor is a typed view into a bufferView.
		/// </summary>
		public List<Accessor> Accessors;

		/// <summary>
		/// An array of keyframe animations.
		/// </summary>
		public List<Animation> Animations;

		/// <summary>
		/// Metadata about the glTF asset.
		/// </summary>
		public Asset Asset;

		/// <summary>
		/// An array of buffers. A buffer points to binary geometry, animation, or skins.
		/// </summary>
		public List<Buffer> Buffers;

		/// <summary>
		/// An array of bufferViews.
		/// A bufferView is a view into a buffer generally representing a subset of the buffer.
		/// </summary>
		public List<BufferView> BufferViews;

		/// <summary>
		/// An array of cameras. A camera defines a projection matrix.
		/// </summary>
		public List<Camera> Cameras;

		/// <summary>
		/// An array of images. An image defines data used to create a texture.
		/// </summary>
		public List<Image> Images;

		/// <summary>
		/// An array of materials. A material defines the appearance of a primitive.
		/// </summary>
		public List<Material> Materials;

		/// <summary>
		/// An array of meshes. A mesh is a set of primitives to be rendered.
		/// </summary>
		public List<Mesh> Meshes;

		/// <summary>
		/// An array of nodes.
		/// </summary>
		public List<Node> Nodes;

		/// <summary>
		/// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
		/// </summary>
		public List<Sampler> Samplers;

		/// <summary>
		/// The index of the default scene.
		/// </summary>
		public SceneId Scene;

		/// <summary>
		/// An array of scenes.
		/// </summary>
		public List<Scene> Scenes;

		/// <summary>
		/// An array of skins. A skin is defined by joints and matrices.
		/// </summary>
		public List<Skin> Skins;

		/// <summary>
		/// An array of textures.
		/// </summary>
		public List<Texture> Textures;

		/// <summary>
		/// Return the default scene. When scene is null, scene of index 0 will be returned.
		/// When scenes list is null or empty, returns null.
		/// </summary>
		public Scene GetDefaultScene()
		{
			if (Scene != null)
			{
				return Scene.Value;
			}

			if (Scenes.Count > 0)
			{
				return Scenes[0];
			}

			return null;
		}

		public static GLTFRoot Deserialize(TextReader textReader)
		{
			var jsonReader = new JsonTextReader(textReader);
			var root = new GLTFRoot();

			if (jsonReader.Read() && jsonReader.TokenType != JsonToken.StartObject)
			{
				throw new Exception("gltf json must be an object");
			}

			while (jsonReader.Read() && jsonReader.TokenType == JsonToken.PropertyName)
			{
				var curProp = jsonReader.Value.ToString();

				switch (curProp)
				{
					case "extensionsUsed":
						root.ExtensionsUsed = jsonReader.ReadStringList();
						break;
					case "extensionsRequired":
						root.ExtensionsRequired = jsonReader.ReadStringList();
						break;
					case "accessors":
						root.Accessors = jsonReader.ReadList(() => Accessor.Deserialize(root, jsonReader));
						break;
					case "animations":
						root.Animations = jsonReader.ReadList(() => Animation.Deserialize(root, jsonReader));
						break;
					case "asset":
						root.Asset = Asset.Deserialize(root, jsonReader);
						break;
					case "buffers":
						root.Buffers = jsonReader.ReadList(() => Buffer.Deserialize(root, jsonReader));
						break;
					case "bufferViews":
						root.BufferViews = jsonReader.ReadList(() => BufferView.Deserialize(root, jsonReader));
						break;
					case "cameras":
						root.Cameras = jsonReader.ReadList(() => Camera.Deserialize(root, jsonReader));
						break;
					case "images":
						root.Images = jsonReader.ReadList(() => Image.Deserialize(root, jsonReader));
						break;
					case "materials":
						root.Materials = jsonReader.ReadList(() => Material.Deserialize(root, jsonReader));
						break;
					case "meshes":
						root.Meshes = jsonReader.ReadList(() => Mesh.Deserialize(root, jsonReader));
						break;
					case "nodes":
						root.Nodes = jsonReader.ReadList(() => Node.Deserialize(root, jsonReader));
						break;
					case "samplers":
						root.Samplers = jsonReader.ReadList(() => Sampler.Deserialize(root, jsonReader));
						break;
					case "scene":
						root.Scene = SceneId.Deserialize(root, jsonReader);
						break;
					case "scenes":
						root.Scenes = jsonReader.ReadList(() => GLTF.Schema.Scene.Deserialize(root, jsonReader));
						break;
					case "skins":
						root.Skins = jsonReader.ReadList(() => Skin.Deserialize(root, jsonReader));
						break;
					case "textures":
						root.Textures = jsonReader.ReadList(() => Texture.Deserialize(root, jsonReader));
						break;
					default:
						root.DefaultPropertyDeserializer(root, jsonReader);
						break;
				}
			}

			return root;
		}

		public void Serialize(TextWriter textWriter)
		{
			JsonWriter jsonWriter = new JsonTextWriter(textWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();

			if (ExtensionsUsed != null && ExtensionsUsed.Count > 0)
			{
				jsonWriter.WritePropertyName("extensionsUsed");
				jsonWriter.WriteStartArray();
				foreach (var extension in ExtensionsUsed)
				{
					jsonWriter.WriteValue(extension);
				}
				jsonWriter.WriteEndArray();
			}

			if (ExtensionsRequired != null && ExtensionsRequired.Count > 0)
			{
				jsonWriter.WritePropertyName("extensionsRequired");
				jsonWriter.WriteStartArray();
				foreach (var extension in ExtensionsRequired)
				{
					jsonWriter.WriteValue(extension);
				}
				jsonWriter.WriteEndArray();
			}

			if (Accessors != null && Accessors.Count > 0)
			{
				jsonWriter.WritePropertyName("accessors");
				jsonWriter.WriteStartArray();
				foreach (var accessor in Accessors)
				{
					accessor.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Animations != null && Animations.Count > 0)
			{
				jsonWriter.WritePropertyName("animations");
				jsonWriter.WriteStartArray();
				foreach (var animation in Animations)
				{
					animation.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			jsonWriter.WritePropertyName("asset");
			Asset.Serialize(jsonWriter);

			if (Buffers != null && Buffers.Count > 0)
			{
				jsonWriter.WritePropertyName("buffers");
				jsonWriter.WriteStartArray();
				foreach (var buffer in Buffers)
				{
					buffer.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (BufferViews != null && BufferViews.Count > 0)
			{
				jsonWriter.WritePropertyName("bufferViews");
				jsonWriter.WriteStartArray();
				foreach (var bufferView in BufferViews)
				{
					bufferView.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Cameras != null && Cameras.Count > 0)
			{
				jsonWriter.WritePropertyName("cameras");
				jsonWriter.WriteStartArray();
				foreach (var camera in Cameras)
				{
					camera.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Images != null && Images.Count > 0)
			{
				jsonWriter.WritePropertyName("images");
				jsonWriter.WriteStartArray();
				foreach (var image in Images)
				{
					image.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Materials != null && Materials.Count > 0)
			{
				jsonWriter.WritePropertyName("materials");
				jsonWriter.WriteStartArray();
				foreach (var material in Materials)
				{
					material.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Meshes != null && Meshes.Count > 0)
			{
				jsonWriter.WritePropertyName("meshes");
				jsonWriter.WriteStartArray();
				foreach (var mesh in Meshes)
				{
					mesh.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Nodes != null && Nodes.Count > 0)
			{
				jsonWriter.WritePropertyName("nodes");
				jsonWriter.WriteStartArray();
				foreach (var node in Nodes)
				{
					node.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Samplers != null && Samplers.Count > 0)
			{
				jsonWriter.WritePropertyName("samplers");
				jsonWriter.WriteStartArray();
				foreach (var sampler in Samplers)
				{
					sampler.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Scene != null)
			{
				jsonWriter.WritePropertyName("scene");
				Scene.Serialize(jsonWriter);
			}

			if (Scenes != null && Scenes.Count > 0)
			{
				jsonWriter.WritePropertyName("scenes");
				jsonWriter.WriteStartArray();
				foreach (var scene in Scenes)
				{
					scene.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Skins != null && Skins.Count > 0)
			{
				jsonWriter.WritePropertyName("skins");
				jsonWriter.WriteStartArray();
				foreach (var skin in Skins)
				{
					skin.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			if (Textures != null && Textures.Count > 0)
			{
				jsonWriter.WritePropertyName("textures");
				jsonWriter.WriteStartArray();
				foreach (var texture in Textures)
				{
					texture.Serialize(jsonWriter);
				}
				jsonWriter.WriteEndArray();
			}

			base.Serialize(jsonWriter);

			jsonWriter.WriteEndObject();
		}
	}
}
