using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// Texture sampler properties for filtering and wrapping modes.
    /// </summary>
    public class GLTFSampler : GLTFChildOfRootProperty
    {
        /// <summary>
        /// Magnification filter.
        /// Valid values correspond to WebGL enums: `9728` (NEAREST) and `9729` (LINEAR).
        /// </summary>
        public GLTFMagFilterMode MagFilter = GLTFMagFilterMode.Linear;

        /// <summary>
        /// Minification filter. All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFMinFilterMode MinFilter = GLTFMinFilterMode.NearestMipmapLinear;

        /// <summary>
        /// s wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFWrapMode WrapS = GLTFWrapMode.Repeat;

        /// <summary>
        /// t wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFWrapMode WrapT = GLTFWrapMode.Repeat;

        public static GLTFSampler Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var sampler = new GLTFSampler();
            
            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "magFilter":
                        sampler.MagFilter = (GLTFMagFilterMode) reader.ReadAsInt32();
                        break;
                    case "minFilter":
                        sampler.MinFilter = (GLTFMinFilterMode)reader.ReadAsInt32();
                        break;
                    case "wrapS":
                        sampler.WrapS = (GLTFWrapMode)reader.ReadAsInt32();
                        break;
                    case "wrapT":
                        sampler.WrapT = (GLTFWrapMode)reader.ReadAsInt32();
                        break;
					default:
						sampler.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return sampler;
        }

        public override void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();

            if (MagFilter != GLTFMagFilterMode.Linear)
            {
                writer.WritePropertyName("magFilter");
                writer.WriteValue((int)MagFilter);
            }

            if (MinFilter != GLTFMinFilterMode.NearestMipmapLinear)
            {
                writer.WritePropertyName("minFilter");
                writer.WriteValue((int)MinFilter);  
            }

            if (WrapS != GLTFWrapMode.Repeat)
            {
                writer.WritePropertyName("WrapS");
                writer.WriteValue((int)WrapS);
            }

            if (WrapT != GLTFWrapMode.Repeat)
            {
                writer.WritePropertyName("WrapT");
                writer.WriteValue((int)WrapT);        
            }

            base.Serialize(writer);
            
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Magnification filter mode.
    /// </summary>
    public enum GLTFMagFilterMode
    {
        None = 0,
        Nearest = 9728,
        Linear = 9729,
    }

    /// <summary>
    /// Minification filter mode.
    /// </summary>
    public enum GLTFMinFilterMode
    {
        None = 0,
        Nearest = 9728,
        Linear = 9729,
        NearestMipmapNearest = 9984,
        LinearMipmapNearest = 9985,
        NearestMipmapLinear = 9986,
        LinearMipmapLinear = 9987
    }

    /// <summary>
    /// Texture wrap mode.
    /// </summary>
    public enum GLTFWrapMode
    {
        None = 0,
        ClampToEdge = 33071,
        MirroredRepeat = 33648,
        Repeat = 10497
    }
}
