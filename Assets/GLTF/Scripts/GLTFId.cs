using System;
using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{

    /// <summary>
    /// Abstract class that stores a reference to the root GLTF object and an id
    /// of an object of type T inside it.
    /// </summary>
    /// <typeparam name="T">The value type returned by the GLTFId reference.</typeparam>
    public abstract class GLTFId<T>
    {
        public int id;
        [System.NonSerialized]
        public GLTFRoot root;
        public abstract T Value { get; }
    }

    [System.Serializable]
    public class GLTFAccessorId : GLTFId<GLTFAccessor>
    {
        public override GLTFAccessor Value
        {
            get { return root.accessors[id]; }
        }

        public static GLTFAccessorId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFAccessorId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFBufferId : GLTFId<GLTFBuffer>
    {
        public override GLTFBuffer Value
        {
            get { return root.buffers[id]; }
        }

        public static GLTFBufferId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFBufferId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFBufferViewId: GLTFId<GLTFBufferView>
    {
        public override GLTFBufferView Value
        {
            get { return root.bufferViews[id]; }
        }

        public static GLTFBufferViewId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFBufferViewId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFCameraId : GLTFId<GLTFCamera>
    {
        public override GLTFCamera Value
        {
            get { return root.cameras[id]; }
        }

        public static GLTFCameraId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFCameraId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFImageId : GLTFId<GLTFImage>
    {
        public override GLTFImage Value
        {
            get { return root.images[id]; }
        }

        public static GLTFImageId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFImageId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFMaterialId : GLTFId<GLTFMaterial>
    {
        public override GLTFMaterial Value
        {
            get { return root.materials[id]; }
        }

        public static GLTFMaterialId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFMaterialId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFMeshId : GLTFId<GLTFMesh>
    {
        public override GLTFMesh Value
        {
            get { return root.meshes[id]; }
        }

        public static GLTFMeshId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFMeshId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFNodeId : GLTFId<GLTFNode>
    {
        public override GLTFNode Value
        {
            get  { return root.nodes[id]; }
        }

        public static GLTFNodeId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFNodeId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }

        public static List<GLTFNodeId> ReadList(GLTFRoot root, JsonTextReader reader)
        {
            if (reader.Read() && reader.TokenType != JsonToken.StartArray)
            {
                throw new Exception("Invalid array.");
            }

            var list = new List<GLTFNodeId>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                var node = new GLTFNodeId
                {
                    id = int.Parse(reader.Value.ToString()),
                    root = root
                };

                list.Add(node);
            }

            return list;
        }
    }

    [System.Serializable]
    public class GLTFSamplerId : GLTFId<GLTFSampler>
    {
        public override GLTFSampler Value
        {
            get { return root.samplers[id]; }
        }

        public static GLTFSamplerId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFSamplerId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFSceneId : GLTFId<GLTFScene>
    {
        public override GLTFScene Value
        {
            get { return root.scenes[id]; }
        }

        public static GLTFSceneId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFSceneId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFSkinId : GLTFId<GLTFSkin>
    {
        public override GLTFSkin Value
        {
            get { return root.skins[id];}
        }

        public static GLTFSkinId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFSkinId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }

    [System.Serializable]
    public class GLTFTextureId : GLTFId<GLTFTexture>
    {
        public override GLTFTexture Value
        {
            get { return root.textures[id]; }
        }

        public static GLTFTextureId Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            return new GLTFTextureId
            {
                id = reader.ReadAsInt32().Value,
                root = root
            };
        }
    }
}
