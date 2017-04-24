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
    [System.Serializable]
    public class GLTFRoot : GLTFProperty
    {
        public string gltfPath;

        /// <summary>
        /// Names of glTF extensions used somewhere in this asset.
        /// </summary>
        public List<string> extensionsUsed;

        /// <summary>
        /// Names of glTF extensions required to properly load this asset.
        /// </summary>
        public List<string> extensionsRequired;

        /// <summary>
        /// An array of accessors. An accessor is a typed view into a bufferView.
        /// </summary>
        public List<GLTFAccessor> accessors;

        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        public List<GLTFAnimation> animations;

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        public GLTFAsset asset;

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        public List<GLTFBuffer> buffers;

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        public List<GLTFBufferView> bufferViews;

        /// <summary>
        /// An array of cameras. A camera defines a projection matrix.
        /// </summary>
        public List<GLTFCamera> cameras;

        /// <summary>
        /// An array of images. An image defines data used to create a texture.
        /// </summary>
        public List<GLTFImage> images;

        /// <summary>
        /// An array of materials. A material defines the appearance of a primitive.
        /// </summary>
        public List<GLTFMaterial> materials;

        /// <summary>
        /// An array of meshes. A mesh is a set of primitives to be rendered.
        /// </summary>
        public List<GLTFMesh> meshes;

        /// <summary>
        /// An array of nodes.
        /// </summary>
        public List<GLTFNode> nodes;

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        public List<GLTFSampler> samplers;

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        public GLTFSceneId scene;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        public List<GLTFScene> scenes;

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        public List<GLTFSkin> skins;

        /// <summary>
        /// An array of textures.
        /// </summary>
        public List<GLTFTexture> textures;

        public static GLTFRoot Deserialize(string gltfPath, JsonTextReader reader)
        {
            var root = new GLTFRoot { gltfPath = gltfPath };

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
                        root.extensionsUsed = reader.ReadStringList();
                        break;
                    case "extensionsRequired":
                        root.extensionsRequired = reader.ReadStringList();
                        break;
                    case "accessors":
                        root.accessors = reader.ReadList(() => GLTFAccessor.Deserialize(root, reader));
                        break;
                    case "animations":
                        root.animations = reader.ReadList(() => GLTFAnimation.Deserialize(root, reader));
                        break;
                    case "asset":
                        root.asset = GLTFAsset.Deserialize(reader);
                        break;
                    case "buffers":
                        root.buffers = reader.ReadList(() => GLTFBuffer.Deserialize(root, reader));
                        break;
                    case "bufferViews":
                        root.bufferViews = reader.ReadList(() => GLTFBufferView.Deserialize(root, reader));
                        break;
                    case "cameras":
                        root.cameras = reader.ReadList(() => GLTFCamera.Deserialize(root, reader));
                        break;
                    case "images":
                        root.images = reader.ReadList(() => GLTFImage.Deserialize(root, reader));
                        break;
                    case "materials":
                        root.materials = reader.ReadList(() => GLTFMaterial.Deserialize(root, reader));
                        break;
                    case "meshes":
                        root.meshes = reader.ReadList(() => GLTFMesh.Deserialize(root, reader));
                        break;
                    case "nodes":
                        root.nodes = reader.ReadList(() => GLTFNode.Deserialize(root, reader));
                        break;
                    case "samplers":
                        root.samplers = reader.ReadList(() => GLTFSampler.Deserialize(root, reader));
                        break;
                    case "scene":
                        root.scene = GLTFSceneId.Deserialize(root, reader);
                        break;
                    case "scenes":
                        root.scenes = reader.ReadList(() => GLTFScene.Deserialize(root, reader));
                        break;
                    case "skins":
                        root.skins = reader.ReadList(() => GLTFSkin.Deserialize(root, reader));
                        break;
                    case "textures":
                        root.textures = reader.ReadList(() => GLTFTexture.Deserialize(root, reader));
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return root;
        }
    }
}
