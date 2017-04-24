using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GLTF
{
    /// <summary>
    /// A texture and its sampler.
    /// </summary>
    [System.Serializable]
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
        public GLTFTextureFormat format = GLTFTextureFormat.RGBA;

        /// <summary>
        /// The texture's internal format.  Valid values correspond to WebGL enums:
        /// `6406` (ALPHA)
        /// `6407` (RGB)
        /// `6408` (RGBA)
        /// `6409` (LUMINANCE)
        /// `6410` (LUMINANCE_ALPHA)
        /// </summary>
        public GLTFTextureFormat internalFormat = GLTFTextureFormat.RGBA;

        /// <summary>
        /// The index of the sampler used by this texture.
        /// </summary>
        public GLTFSamplerId sampler;

        /// <summary>
        /// The index of the image used by this texture.
        /// </summary>
        public GLTFImageId source;

        /// <summary>
        /// The target that the WebGL texture should be bound to.
        /// Valid values correspond to WebGL enums: `3553` (TEXTURE_2D).
        /// </summary>
        public GLTFTextureTarget target = GLTFTextureTarget.TEXTURE_2D;

        /// <summary>
        /// Texel datatype.
        /// Valid values correspond to WebGL enums:
        /// `5121` (UNSIGNED_BYTE)
        /// `33635` (UNSIGNED_SHORT_5_6_5)
        /// `32819` (UNSIGNED_SHORT_4_4_4_4)
        /// `32820` (UNSIGNED_SHORT_5_5_5_1)
        /// </summary>
        public GLTFTexelDataType type = GLTFTexelDataType.UNSIGNED_BYTE;

        /// <summary>
        /// Return or create the GLTFTexture's Texture object.
        /// </summary>
        public Texture2D Texture
        {
            get { return source.Value.texture; }
        }

        public static GLTFTexture Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var texture = new GLTFTexture();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "format":
                        texture.format = (GLTFTextureFormat) reader.ReadAsInt32().Value;
                        break;
                    case "internalFormat":
                        texture.internalFormat = (GLTFTextureFormat) reader.ReadAsInt32().Value;
                        break;
                    case "sampler":
                        texture.sampler = GLTFSamplerId.Deserialize(root, reader);
                        break;
                    case "source":
                        texture.source = GLTFImageId.Deserialize(root, reader);
                        break;
                    case "target":
                        texture.target = (GLTFTextureTarget) reader.ReadAsInt32().Value;
                        break;
                    case "type":
                        texture.type = (GLTFTexelDataType) reader.ReadAsInt32().Value;
                        break;
                    case "name":
                        texture.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
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
        ALPHA = 6406,
        RGB = 6407,
        RGBA = 6408,
        LUMINANCE = 6409,
        LUMINANCE_ALPHA = 6410
    }

    /// <summary>
    /// The target that the WebGL texture should be bound to.
    /// </summary>
    public enum GLTFTextureTarget
    {
        TEXTURE_2D = 3553
    }

    /// <summary>
    /// Texel datatype.
    /// </summary>
    public enum GLTFTexelDataType
    {
        UNSIGNED_BYTE = 5121,
        UNSIGNED_SHORT_5_6_5 = 33635,
        UNSIGNED_SHORT_4_4_4_4 = 32819,
        UNSIGNED_SHORT_5_5_5_1 = 32820
    }
}
