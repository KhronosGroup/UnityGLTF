using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GLTF
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

        public void Serialize(JsonWriter writer)
        {
            writer.WriteValue(Id);
        }
    }

    public class GLTFAccessorId : GLTFId<GLTFAccessor>
    {
        public override GLTFAccessor Value
        {
            get { return Root.Accessors[Id]; }
        }

        public static GLTFAccessorId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFAccessorId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFBufferId : GLTFId<GLTFBuffer>
    {
        public override GLTFBuffer Value
        {
            get { return Root.Buffers[Id]; }
        }

        public static GLTFBufferId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFBufferId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFBufferViewId : GLTFId<GLTFBufferView>
    {
        public override GLTFBufferView Value
        {
            get { return Root.BufferViews[Id]; }
        }

        public static GLTFBufferViewId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFBufferViewId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFCameraId : GLTFId<GLTFCamera>
    {
        public override GLTFCamera Value
        {
            get { return Root.Cameras[Id]; }
        }

        public static GLTFCameraId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFCameraId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFImageId : GLTFId<GLTFImage>
    {
        public override GLTFImage Value
        {
            get { return Root.Images[Id]; }
        }

        public static GLTFImageId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFImageId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFMaterialId : GLTFId<GLTFMaterial>
    {
        public override GLTFMaterial Value
        {
            get { return Root.Materials[Id]; }
        }

        public static GLTFMaterialId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFMaterialId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFMeshId : GLTFId<GLTFMesh>
    {
        public override GLTFMesh Value
        {
            get { return Root.Meshes[Id]; }
        }

        public static GLTFMeshId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFMeshId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFNodeId : GLTFId<GLTFNode>
    {
        public override GLTFNode Value
        {
            get { return Root.Nodes[Id]; }
        }

        public static GLTFNodeId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFNodeId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }

        public static List<GLTFNodeId> ReadList(GLTFRoot root, JsonReader reader)
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
                    Id = int.Parse(reader.Value.ToString()),
                    Root = root
                };

                list.Add(node);
            }

            return list;
        }
    }

    public class GLTFSamplerId : GLTFId<GLTFSampler>
    {
        public override GLTFSampler Value
        {
            get { return Root.Samplers[Id]; }
        }

        public static GLTFSamplerId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFSamplerId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFSceneId : GLTFId<GLTFScene>
    {
        public override GLTFScene Value
        {
            get { return Root.Scenes[Id]; }
        }

        public static GLTFSceneId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFSceneId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFSkinId : GLTFId<GLTFSkin>
    {
        public override GLTFSkin Value
        {
            get { return Root.Skins[Id]; }
        }

        public static GLTFSkinId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFSkinId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }

    public class GLTFTextureId : GLTFId<GLTFTexture>
    {
        public override GLTFTexture Value
        {
            get { return Root.Textures[Id]; }
        }

        public static GLTFTextureId Deserialize(GLTFRoot root, JsonReader reader)
        {
            return new GLTFTextureId
            {
                Id = reader.ReadAsInt32().Value,
                Root = root
            };
        }
    }
}
