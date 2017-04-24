using System;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GLTF
{
    /// <summary>
    /// A camera's projection.  A node can reference a camera to apply a transform
    /// to place the camera in the scene
    /// </summary>
    [System.Serializable]
    public class GLTFCamera
    {
        /// <summary>
        /// An orthographic camera containing properties to create an orthographic
        /// projection matrix.
        /// </summary>
        public GLTFCameraOrthographic orthographic;

        /// <summary>
        /// A perspective camera containing properties to create a perspective
        /// projection matrix.
        /// </summary>
        public GLTFCameraPerspective perspective;

        /// <summary>
        /// Specifies if the camera uses a perspective or orthographic projection.
        /// Based on this, either the camera's `perspective` or `orthographic` property
        /// will be defined.
        /// </summary>
        public GLTFCameraType type;

        public static GLTFCamera Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var camera = new GLTFCamera();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "orthographic":
                        camera.orthographic = GLTFCameraOrthographic.Deserialize(root, reader);
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return camera;
        }
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
        public double xmag;

        /// <summary>
        /// The floating-point vertical magnification of the view.
        /// </summary>
        public double ymag;

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// </summary>
        public double zfar;

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        public double znear;

        public static GLTFCameraOrthographic Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var cameraOrthographic = new GLTFCameraOrthographic();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Orthographic camera must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "xmag":
                        cameraOrthographic.xmag = reader.ReadAsDouble().Value;
                        break;
                    case "ymag":
                        cameraOrthographic.ymag = reader.ReadAsDouble().Value;
                        break;
                    case "zfar":
                        cameraOrthographic.zfar = reader.ReadAsDouble().Value;
                        break;
                    case "znear":
                        cameraOrthographic.znear = reader.ReadAsDouble().Value;
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return cameraOrthographic;
        }
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
        public double znear;

        public static GLTFCameraPerspective Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var cameraPerspective = new GLTFCameraPerspective();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Perspective camera must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "aspectRatio":
                        cameraPerspective.aspectRatio = reader.ReadAsDouble().Value;
                        break;
                    case "yfov":
                        cameraPerspective.yfov = reader.ReadAsDouble().Value;
                        break;
                    case "zfar":
                        cameraPerspective.zfar = reader.ReadAsDouble().Value;
                        break;
                    case "znear":
                        cameraPerspective.znear = reader.ReadAsDouble().Value;
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return cameraPerspective;
        }
    }

    public enum GLTFCameraType
    {
        perspective,
        orthographic
    }
}
