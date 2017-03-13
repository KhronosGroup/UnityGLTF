using System;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Create an object that we can use to reference the GLTF root
    /// from any GLTFId. Used to access the objects referenced by the id.
    /// It allows us to main type safety and do error handling for missing
    /// references.
    /// </summary>
    public class GLTFRootRef
    {
        public GLTFRoot root;
    }

    /// <summary>
    /// Abstract class that stores a reference to the root GLTF object and an id
    /// of an object of type T inside it.
    /// </summary>
    /// <typeparam name="T">The value type returned by the GLTFId reference.</typeparam>
    public abstract class GLTFId<T>
    {
        public int id;
        public GLTFRootRef rootRef;
        public abstract T Value { get; }
    }

    /// <summary>
    /// Convert from JSON number values to GLTFId instances and back.
    /// </summary>
    /// <typeparam name="T">The subclass of GLTFId.</typeparam>
    /// <typeparam name="V">The value type returned by the GLTFId reference.</typeparam>
    public class GLTFIdConverter<T, V> : JsonConverter where T: GLTFId<V>, new()
    {
        private GLTFRootRef rootRef;

        public GLTFIdConverter(GLTFRootRef rootRef) {
            this.rootRef = rootRef;
        }

        /// <summary>
        /// This converter will be used if the object is of type T.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        /// <summary>
        /// Parse the GLTFId from a JSON number value.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.Integer)
            {
                throw new Exception("Invalid Id type.");
            }

            int id = serializer.Deserialize<int>(reader);

            T idRef = new T();
            idRef.id = id;
            idRef.rootRef = rootRef;

            return idRef;
        }

        /// <summary>
        /// Write the GLTFId to JSON as a number.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            T gltfId = (T)value;
            serializer.Serialize(writer, gltfId.id);
        }
    }

    public class GLTFAccessorId : GLTFId<GLTFAccessor>
    {
        override public GLTFAccessor Value
        {
            get {
                return rootRef.root.accessors[id];
            }
        }
    }

    public class GLTFAccessorIdConverter : GLTFIdConverter<GLTFAccessorId, GLTFAccessor>
    {
        public GLTFAccessorIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFBufferId : GLTFId<GLTFBuffer>
    {
        override public GLTFBuffer Value
        {
            get
            {
                return rootRef.root.buffers[id];
            }
        }
    }

    public class GLTFBufferIdConverter : GLTFIdConverter<GLTFBufferId, GLTFBuffer>
    {
        public GLTFBufferIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFBufferViewId: GLTFId<GLTFBufferView>
    {
        override public GLTFBufferView Value
        {
            get
            {
                return rootRef.root.bufferViews[id];
            }
        }
    }

    public class GLTFBufferViewIdConverter : GLTFIdConverter<GLTFBufferViewId, GLTFBufferView>
    {
        public GLTFBufferViewIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFCameraId : GLTFId<GLTFCamera>
    {
        override public GLTFCamera Value
        {
            get
            {
                return rootRef.root.cameras[id];
            }
        }
    }

    public class GLTFCameraIdConverter : GLTFIdConverter<GLTFCameraId, GLTFCamera>
    {
        public GLTFCameraIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFImageId : GLTFId<GLTFImage>
    {
        override public GLTFImage Value
        {
            get
            {
                return rootRef.root.images[id];
            }
        }
    }

    public class GLTFImageIdConverter : GLTFIdConverter<GLTFImageId, GLTFImage>
    {
        public GLTFImageIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFMaterialId : GLTFId<GLTFMaterial>
    {
        override public GLTFMaterial Value
        {
            get
            {
                return rootRef.root.materials[id];
            }
        }
    }

    public class GLTFMaterialIdConverter : GLTFIdConverter<GLTFMaterialId, GLTFMaterial>
    {
        public GLTFMaterialIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFMeshId : GLTFId<GLTFMesh>
    {
        override public GLTFMesh Value
        {
            get
            {
                return rootRef.root.meshes[id];
            }
        }
    }

    public class GLTFMeshIdConverter : GLTFIdConverter<GLTFMeshId, GLTFMesh>
    {
        public GLTFMeshIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFNodeId : GLTFId<GLTFNode>
    {
        override public GLTFNode Value
        {
            get
            {
                return rootRef.root.nodes[id];
            }
        }
    }

    public class GLTFNodeIdConverter : GLTFIdConverter<GLTFNodeId, GLTFNode>
    {
        public GLTFNodeIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFSamplerId : GLTFId<GLTFSampler>
    {
        override public GLTFSampler Value
        {
            get
            {
                return rootRef.root.samplers[id];
            }
        }
    }

    public class GLTFSamplerIdConverter : GLTFIdConverter<GLTFSamplerId, GLTFSampler>
    {
        public GLTFSamplerIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFSceneId : GLTFId<GLTFScene>
    {
        override public GLTFScene Value
        {
            get
            {
                return rootRef.root.scenes[id];
            }
        }
    }

    public class GLTFSceneIdConverter : GLTFIdConverter<GLTFSceneId, GLTFScene>
    {
        public GLTFSceneIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFSkinId : GLTFId<GLTFSkin>
    {
        override public GLTFSkin Value
        {
            get
            {
                return rootRef.root.skins[id];
            }
        }
    }

    public class GLTFSkinIdConverter : GLTFIdConverter<GLTFSkinId, GLTFSkin>
    {
        public GLTFSkinIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }

    public class GLTFTextureId : GLTFId<GLTFTexture>
    {
        override public GLTFTexture Value
        {
            get
            {
                return rootRef.root.textures[id];
            }
        }
    }

    public class GLTFTextureIdConverter : GLTFIdConverter<GLTFTextureId, GLTFTexture>
    {
        public GLTFTextureIdConverter(GLTFRootRef rootRef) : base(rootRef)
        {
        }
    }
}
