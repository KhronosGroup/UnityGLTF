using System.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// A texture and its sampler.
    /// </summary>
    public class GLTFTexture
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
        [JsonProperty(Required = Required.Always)]
        public GLTFSamplerId sampler;

        /// <summary>
        /// The index of the image used by this texture.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
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

        public string name;

        // Stored reference to the Texture so we don't have to regenerate it if
        // used in multiple materials.
        private Texture2D texture;

        /// <summary>
        /// Return or create the GLTFTexture's Texture object.
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                if (texture == null)
                {
                    texture = new Texture2D(0, 0);
                    texture.LoadImage(source.Value.Data);
                }

                return texture;
            }
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
