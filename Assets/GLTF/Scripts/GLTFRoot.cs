using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// The root object for a glTF asset.
    /// </summary>
    public class GLTFRoot : GLTFProperty
    {
	    public GLTFRoot(string url)
	    {
			_url = url;
	    }

        private readonly string _url;

	    public string Url
	    {
		    get { return _url; }
	    }

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
        public List<GLTFAccessor> Accessors;

        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        public List<GLTFAnimation> Animations;

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        public GLTFAsset Asset;

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        public List<GLTFBuffer> Buffers;

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        public List<GLTFBufferView> BufferViews;

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
        public List<GLTFNode> Nodes;

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        public List<GLTFSampler> Samplers;

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        public GLTFSceneId Scene;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        public List<GLTFScene> Scenes;

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        public List<GLTFSkin> Skins;

        /// <summary>
        /// An array of textures.
        /// </summary>
        public List<GLTFTexture> Textures;

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

		    if (Scenes != null && Scenes.Count > 0)
		    {
			    return Scenes[0];
		    }

		    return null;
	    }

        public static GLTFRoot Deserialize(string url, JsonTextReader reader)
        {
            var root = new GLTFRoot(url);

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("gltf json must be an object");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "extensionsUsed":
                        root.ExtensionsUsed = reader.ReadStringList();
                        break;
                    case "extensionsRequired":
                        root.ExtensionsRequired = reader.ReadStringList();
                        break;
                    case "accessors":
                        root.Accessors = reader.ReadList(() => GLTFAccessor.Deserialize(root, reader));
                        break;
                    case "animations":
                        root.Animations = reader.ReadList(() => GLTFAnimation.Deserialize(root, reader));
                        break;
                    case "asset":
                        root.Asset = GLTFAsset.Deserialize(root, reader);
                        break;
                    case "buffers":
                        root.Buffers = reader.ReadList(() => GLTFBuffer.Deserialize(root, reader));
                        break;
                    case "bufferViews":
                        root.BufferViews = reader.ReadList(() => GLTFBufferView.Deserialize(root, reader));
                        break;
                    case "cameras":
                        root.Cameras = reader.ReadList(() => GLTFCamera.Deserialize(root, reader));
                        break;
                    case "images":
                        root.Images = reader.ReadList(() => GLTFImage.Deserialize(root, reader));
                        break;
                    case "materials":
                        root.Materials = reader.ReadList(() => GLTFMaterial.Deserialize(root, reader));
                        break;
                    case "meshes":
                        root.Meshes = reader.ReadList(() => GLTFMesh.Deserialize(root, reader));
                        break;
                    case "nodes":
                        root.Nodes = reader.ReadList(() => GLTFNode.Deserialize(root, reader));
                        break;
                    case "samplers":
                        root.Samplers = reader.ReadList(() => GLTFSampler.Deserialize(root, reader));
                        break;
                    case "scene":
                        root.Scene = GLTFSceneId.Deserialize(root, reader);
                        break;
                    case "scenes":
                        root.Scenes = reader.ReadList(() => GLTFScene.Deserialize(root, reader));
                        break;
                    case "skins":
                        root.Skins = reader.ReadList(() => GLTFSkin.Deserialize(root, reader));
                        break;
                    case "textures":
                        root.Textures = reader.ReadList(() => GLTFTexture.Deserialize(root, reader));
                        break;
					default:
						root.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return root;
        }
    }
}
