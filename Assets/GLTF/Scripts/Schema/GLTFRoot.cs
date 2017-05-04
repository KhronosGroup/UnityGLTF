using System;
using System.Collections.Generic;
using System.IO;
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

        public static GLTFRoot Deserialize(JsonReader reader)
        {
            var root = new GLTFRoot();

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

        public override void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();

            if (ExtensionsUsed != null && ExtensionsUsed.Count > 0)
            {
                writer.WritePropertyName("extensionsUsed");
                writer.WriteStartArray();
                foreach (var extension in ExtensionsUsed)
                {
                    writer.WriteValue(extension);
                }
                writer.WriteEndArray();
            }

            if (ExtensionsRequired != null && ExtensionsRequired.Count > 0)
            {
                writer.WritePropertyName("extensionsRequired");
                writer.WriteStartArray();
                foreach (var extension in ExtensionsRequired)
                {
                    writer.WriteValue(extension);
                }
                writer.WriteEndArray();
            }

            if (Accessors != null && Accessors.Count > 0)
            {
                writer.WritePropertyName("accessors");
                writer.WriteStartArray();
                foreach (var accessor in Accessors)
                {
                    accessor.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Animations != null && Animations.Count > 0)
            {
                writer.WritePropertyName("animations");
                writer.WriteStartArray();
                foreach (var animation in Animations)
                {
                    animation.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName("asset");
            Asset.Serialize(writer);

            if (Buffers != null && Buffers.Count > 0)
            {
                writer.WritePropertyName("buffers");
                writer.WriteStartArray();
                foreach (var buffer in Buffers)
                {
                    buffer.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (BufferViews != null && BufferViews.Count > 0)
            {
                writer.WritePropertyName("bufferViews");
                writer.WriteStartArray();
                foreach (var bufferView in BufferViews)
                {
                    bufferView.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Cameras != null && Cameras.Count > 0)
            {
                writer.WritePropertyName("cameras");
                writer.WriteStartArray();
                foreach (var camera in Cameras)
                {
                    camera.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Images != null && Images.Count > 0)
            {
                writer.WritePropertyName("images");
                writer.WriteStartArray();
                foreach (var image in Images)
                {
                    image.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Materials != null && Materials.Count > 0)
            {
                writer.WritePropertyName("materials");
                writer.WriteStartArray();
                foreach (var material in Materials)
                {
                    material.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Meshes != null && Meshes.Count > 0)
            {
                writer.WritePropertyName("meshes");
                writer.WriteStartArray();
                foreach (var mesh in Meshes)
                {
                    mesh.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Nodes != null && Nodes.Count > 0)
            {
                writer.WritePropertyName("nodes");
                writer.WriteStartArray();
                foreach (var node in Nodes)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Samplers != null && Samplers.Count > 0)
            {
                writer.WritePropertyName("samplers");
                writer.WriteStartArray();
                foreach (var sampler in Samplers)
                {
                    sampler.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Scene != null)
            {
                writer.WritePropertyName("scene");
                Scene.Serialize(writer);
            }

            if (Scenes != null && Scenes.Count > 0)
            {
                writer.WritePropertyName("scenes");
                writer.WriteStartArray();
                foreach (var scene in Scenes)
                {
                    scene.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Skins != null && Skins.Count > 0)
            {
                writer.WritePropertyName("skins");
                writer.WriteStartArray();
                foreach (var skin in Skins)
                {
                    skin.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            if (Textures != null && Textures.Count > 0)
            {
                writer.WritePropertyName("textures");
                writer.WriteStartArray();
                foreach (var texture in Textures)
                {
                    texture.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            base.Serialize(writer);

            writer.WriteEndObject();
        }
    }
}
