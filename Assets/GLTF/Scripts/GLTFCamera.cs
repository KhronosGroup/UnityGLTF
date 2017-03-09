using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GLTF
{
    /// <summary>
    /// A camera's projection.  A node can reference a camera to apply a transform
    /// to place the camera in the scene
    /// </summary>
    public class GLTFCamera
    {
        /// <summary>
        /// An orthographic camera containing properties to create an orthographic
        /// projection matrix.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFCameraOrthographic orthographic;

        /// <summary>
        /// A perspective camera containing properties to create a perspective
        /// projection matrix.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFCameraPerspective perspective;

        /// <summary>
        /// Specifies if the camera uses a perspective or orthographic projection.
        /// Based on this, either the camera's `perspective` or `orthographic` property
        /// will be defined.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GLTFCameraType type;
    }

    /// <summary>
    /// An orthographic camera containing properties to create an orthographic
    /// projection matrix.
    /// </summary>
    public class GLTFCameraOrthographic
    {
        /// <summary>
        /// The floating-point horizontal magnification of the view.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double xmag;

        /// <summary>
        /// The floating-point vertical magnification of the view.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double ymag;

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double zfar;

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double znear;
    }

    /// <summary>
    /// A perspective camera containing properties to create a perspective projection
    /// matrix.
    /// </summary>
    public class GLTFCameraPerspective
    {
        /// <summary>
        /// The floating-point aspect ratio of the field of view.
        /// When this is undefined, the aspect ratio of the canvas is used.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double aspectRatio;

        /// <summary>
        /// The floating-point vertical field of view in radians.
        /// <minimum>0.0</minimum>
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double yfov;

        /// <summary>
        /// The floating-point distance to the far clipping plane. When defined,
        /// `zfar` must be greater than `znear`.
        /// If `zfar` is undefined, runtime must use infinite projection matrix.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double zfar;

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// <minimum>0.0</minimum>
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double znear;
    }

    public enum GLTFCameraType
    {
        perspective,
        orthographic
    }
}
