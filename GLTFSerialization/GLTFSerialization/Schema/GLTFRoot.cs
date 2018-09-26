using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public List<GLTFAnimation> Animations;

		/// <summary>
		/// Metadata about the glTF asset.
		/// </summary>
		public Asset Asset;

		/// <summary>
		/// An array of buffers. A buffer points to binary geometry, animation, or skins.
		/// </summary>
		public List<GLTFBuffer> Buffers;

		/// <summary>
		/// An array of bufferViews.
		/// A bufferView is a view into a buffer generally representing a subset of the buffer.
		/// </summary>
		public List<BufferView> BufferViews;

		/// <summary>
		/// An array of cameras. A camera defines a projection matrix.
		/// </summary>
		public List<GLTFCamera> Cameras;

		/// <summary>
		/// An array of images. An image defines data used to create a texture.
		/// </summary>
		public List<GLTFImage> Images;

		/// <summary>
		/// An array of materials. A material defines the appearance of a primitive.
		/// </summary>
		public List<GLTFMaterial> Materials;

		/// <summary>
		/// An array of meshes. A mesh is a set of primitives to be rendered.
		/// </summary>
		public List<GLTFMesh> Meshes;

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
		public List<GLTFScene> Scenes;

		/// <summary>
		/// An array of skins. A skin is defined by joints and matrices.
		/// </summary>
		public List<Skin> Skins;

		/// <summary>
		/// An array of textures.
		/// </summary>
		public List<GLTFTexture> Textures;

		public GLTFRoot()
		{
		}

		public GLTFRoot(GLTFRoot gltfRoot) : base(gltfRoot)
		{
			if (gltfRoot.ExtensionsUsed != null)
			{
				ExtensionsUsed = gltfRoot.ExtensionsUsed.ToList();
			}

			if (gltfRoot.ExtensionsRequired != null)
			{
				ExtensionsRequired = gltfRoot.ExtensionsRequired.ToList();
			}

			if (gltfRoot.Accessors != null)
			{
				Accessors = new List<Accessor>(gltfRoot.Accessors.Count);
				foreach (Accessor accessor in gltfRoot.Accessors)
				{
					Accessors.Add(new Accessor(accessor, this));
				}
			}

			if (gltfRoot.Animations != null)
			{
				Animations = new List<GLTFAnimation>(gltfRoot.Animations.Count);
				foreach (GLTFAnimation animation in gltfRoot.Animations)
				{
					Animations.Add(new GLTFAnimation(animation, this));
				}
			}

			if (gltfRoot.Asset != null)
			{
				Asset = new Asset(gltfRoot.Asset);
			}

			if (gltfRoot.Buffers != null)
			{
				Buffers = new List<GLTFBuffer>(gltfRoot.Buffers.Count);
				foreach (GLTFBuffer buffer in gltfRoot.Buffers)
				{
					Buffers.Add(new GLTFBuffer(buffer, this));
				}
			}

			if (gltfRoot.BufferViews != null)
			{
				BufferViews = new List<BufferView>(gltfRoot.BufferViews.Count);
				foreach (BufferView bufferView in gltfRoot.BufferViews)
				{
					BufferViews.Add(new BufferView(bufferView, this));
				}
			}
			
			if (gltfRoot.Cameras != null)
			{
				Cameras = new List<GLTFCamera>(gltfRoot.Cameras.Count);
				foreach (GLTFCamera camera in gltfRoot.Cameras)
				{
					Cameras.Add(new GLTFCamera(camera, this));
				}
			}

			if (gltfRoot.Images != null)
			{
				Images = new List<GLTFImage>(gltfRoot.Images.Count);
				foreach (GLTFImage image in gltfRoot.Images)
				{
					Images.Add(new GLTFImage(image, this));
				}
			}

			if (gltfRoot.Materials != null)
			{
				Materials = new List<GLTFMaterial>(gltfRoot.Materials.Count);
				foreach (GLTFMaterial material in gltfRoot.Materials)
				{
					Materials.Add(new GLTFMaterial(material, this));
				}
			}

			if (gltfRoot.Meshes != null)
			{
				Meshes = new List<GLTFMesh>(gltfRoot.Meshes.Count);
				foreach (GLTFMesh mesh in gltfRoot.Meshes)
				{
					Meshes.Add(new GLTFMesh(mesh, this));
				}
			}

			if (gltfRoot.Nodes != null)
			{
				Nodes = new List<Node>(gltfRoot.Nodes.Count);
				foreach (Node node in gltfRoot.Nodes)
				{
					Nodes.Add(new Node(node, this));
				}
			}

			if (gltfRoot.Samplers != null)
			{
				Samplers = new List<Sampler>(gltfRoot.Samplers.Count);
				foreach (Sampler sampler in gltfRoot.Samplers)
				{
					Samplers.Add(new Sampler(sampler, this));
				}
			}

			if (gltfRoot.Scene != null)
			{
				Scene = new SceneId(gltfRoot.Scene, this);
			}
			
			if (gltfRoot.Scenes != null)
			{
				Scenes = new List<GLTFScene>(gltfRoot.Scenes.Count);
				foreach (GLTFScene scene in gltfRoot.Scenes)
				{
					Scenes.Add(new GLTFScene(scene, this));
				}
			}
			
			if (gltfRoot.Skins != null)
			{
				Skins = new List<Skin>(gltfRoot.Skins.Count);
				foreach (Skin skin in gltfRoot.Skins)
				{
					Skins.Add(new Skin(skin, this));
				}
			}
			
			if (gltfRoot.Textures != null)
			{
				Textures = new List<GLTFTexture>(gltfRoot.Textures.Count);
				foreach (GLTFTexture texture in gltfRoot.Textures)
				{
					Textures.Add(new GLTFTexture(texture, this));
				}
			}
		}

		/// <summary>
		/// Whether this object is a GLB
		/// </summary>
		public bool IsGLB;

		/// <summary>
		/// Return the default scene. When scene is null, scene of index 0 will be returned.
		/// When scenes list is null or empty, returns null.
		/// </summary>
		public GLTFScene GetDefaultScene()
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
						root.Animations = jsonReader.ReadList(() => GLTFAnimation.Deserialize(root, jsonReader));
						break;
					case "asset":
						root.Asset = Asset.Deserialize(root, jsonReader);
						break;
					case "buffers":
						root.Buffers = jsonReader.ReadList(() => GLTFBuffer.Deserialize(root, jsonReader));
						break;
					case "bufferViews":
						root.BufferViews = jsonReader.ReadList(() => BufferView.Deserialize(root, jsonReader));
						break;
					case "cameras":
						root.Cameras = jsonReader.ReadList(() => GLTFCamera.Deserialize(root, jsonReader));
						break;
					case "images":
						root.Images = jsonReader.ReadList(() => GLTFImage.Deserialize(root, jsonReader));
						break;
					case "materials":
						root.Materials = jsonReader.ReadList(() => GLTFMaterial.Deserialize(root, jsonReader));
						break;
					case "meshes":
						root.Meshes = jsonReader.ReadList(() => GLTFMesh.Deserialize(root, jsonReader));
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
						root.Scenes = jsonReader.ReadList(() => GLTF.Schema.GLTFScene.Deserialize(root, jsonReader));
						break;
					case "skins":
						root.Skins = jsonReader.ReadList(() => Skin.Deserialize(root, jsonReader));
						break;
					case "textures":
						root.Textures = jsonReader.ReadList(() => GLTFTexture.Deserialize(root, jsonReader));
						break;
					default:
						root.DefaultPropertyDeserializer(root, jsonReader);
						break;
				}
			}

			return root;
		}

		public void Serialize(TextWriter textWriter, bool isGLB = false)
		{
			JsonWriter jsonWriter = new JsonTextWriter(textWriter);
			if (isGLB)
			{
				jsonWriter.Formatting = Formatting.None;
			}
			else
			{
				jsonWriter.Formatting = Formatting.Indented;
			}

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
