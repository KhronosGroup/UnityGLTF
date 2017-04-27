using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A texture and its sampler.
    /// </summary>
    public class GLTFTexture : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The texture's format.  Valid values correspond to WebGL enums:
        /// `6406` (ALPHA)
        /// `6407` (RGB)
        /// `6408` (RGBA)
        /// `6409` (LUMINANCE)
        /// `6410` (LUMINANCE_ALPHA)
        /// </summary>
        public GLTFTextureFormat Format = GLTFTextureFormat.Rgba;

        /// <summary>
        /// The texture's internal format.  Valid values correspond to WebGL enums:
        /// `6406` (ALPHA)
        /// `6407` (RGB)
        /// `6408` (RGBA)
        /// `6409` (LUMINANCE)
        /// `6410` (LUMINANCE_ALPHA)
        /// </summary>
        public GLTFTextureFormat InternalFormat = GLTFTextureFormat.Rgba;

        /// <summary>
        /// The index of the sampler used by this texture.
        /// </summary>
        public GLTFSamplerId Sampler;

        /// <summary>
        /// The index of the image used by this texture.
        /// </summary>
        public GLTFImageId Source;

        /// <summary>
        /// The target that the WebGL texture should be bound to.
        /// Valid values correspond to WebGL enums: `3553` (TEXTURE_2D).
        /// </summary>
        public GLTFTextureTarget Target = GLTFTextureTarget.Texture2D;

        /// <summary>
        /// Texel datatype.
        /// Valid values correspond to WebGL enums:
        /// `5121` (UNSIGNED_BYTE)
        /// `33635` (UNSIGNED_SHORT_5_6_5)
        /// `32819` (UNSIGNED_SHORT_4_4_4_4)
        /// `32820` (UNSIGNED_SHORT_5_5_5_1)
        /// </summary>
        public GLTFTexelDataType Type = GLTFTexelDataType.UnsignedByte;

        public static GLTFTexture Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var texture = new GLTFTexture();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "format":
                        texture.Format = (GLTFTextureFormat) reader.ReadAsInt32().Value;
                        break;
                    case "internalFormat":
                        texture.InternalFormat = (GLTFTextureFormat) reader.ReadAsInt32().Value;
                        break;
                    case "sampler":
                        texture.Sampler = GLTFSamplerId.Deserialize(root, reader);
                        break;
                    case "source":
                        texture.Source = GLTFImageId.Deserialize(root, reader);
                        break;
                    case "target":
                        texture.Target = (GLTFTextureTarget) reader.ReadAsInt32().Value;
                        break;
                    case "type":
                        texture.Type = (GLTFTexelDataType) reader.ReadAsInt32().Value;
                        break;
					default:
						texture.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return texture;
        }
    }

    /// <summary>
    /// The texture's format.
    /// </summary>
    public enum GLTFTextureFormat
    {
        Alpha = 6406,
        Rgb = 6407,
        Rgba = 6408,
        Luminance = 6409,
        LuminanceAlpha = 6410
    }

    /// <summary>
    /// The target that the WebGL texture should be bound to.
    /// </summary>
    public enum GLTFTextureTarget
    {
        Texture2D = 3553
    }

    /// <summary>
    /// Texel datatype.
    /// </summary>
    public enum GLTFTexelDataType
    {
        UnsignedByte = 5121,
        UnsignedShort_5_6_5 = 33635,
	    UnsignedShort_4_4_4_4 = 32819,
	    UnsignedShort_5_5_5_1 = 32820
    }
}
