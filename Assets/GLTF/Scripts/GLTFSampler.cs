namespace GLTF
{
    /// <summary>
    /// Texture sampler properties for filtering and wrapping modes.
    /// </summary>
    public class GLTFSampler
    {
        /// <summary>
        /// Magnification filter.
        /// Valid values correspond to WebGL enums: `9728` (NEAREST) and `9729` (LINEAR).
        /// </summary>
        public GLTFMagFilterMode magFilter = GLTFMagFilterMode.LINEAR;

        /// <summary>
        /// Minification filter. All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFMinFilterMode minFilter = GLTFMinFilterMode.NEAREST_MIPMAP_LINEAR;

        /// <summary>
        /// s wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFWrapMode wrapS = GLTFWrapMode.REPEAT;

        /// <summary>
        /// t wrapping mode.  All valid values correspond to WebGL enums.
        /// </summary>
        public GLTFWrapMode wrapT = GLTFWrapMode.REPEAT;
    }

    /// <summary>
    /// Magnification filter mode.
    /// </summary>
    public enum GLTFMagFilterMode
    {
        NEAREST = 9728,
        LINEAR = 9729,
    }

    /// <summary>
    /// Minification filter mode.
    /// </summary>
    public enum GLTFMinFilterMode
    {
        NEAREST = 9728,
        LINEAR = 9729,
        NEAREST_MIPMAP_NEAREST = 9984,
        LINEAR_MIPMAP_NEAREST = 9985,
        NEAREST_MIPMAP_LINEAR = 9986,
        LINEAR_MIPMAP_LINEAR = 9987
    }

    /// <summary>
    /// Texture wrap mode.
    /// </summary>
    public enum GLTFWrapMode
    {
        CLAMP_TO_EDGE = 33071,
        MIRRORED_REPEAT = 33648,
        REPEAT = 10497
    }
}
