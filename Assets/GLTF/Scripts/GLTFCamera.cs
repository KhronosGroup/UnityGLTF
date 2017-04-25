using System;
using GLTF.JsonExtensions;
using Newtonsoft.Json;

namespace GLTF
{
    /// <summary>
    /// A camera's projection.  A node can reference a camera to apply a transform
    /// to place the camera in the scene
    /// </summary>
    public class GLTFCamera : GLTFChildOfRootProperty
    {
        /// <summary>
        /// An orthographic camera containing properties to create an orthographic
        /// projection matrix.
        /// </summary>
        public GLTFCameraOrthographic Orthographic;

        /// <summary>
        /// A perspective camera containing properties to create a perspective
        /// projection matrix.
        /// </summary>
        public GLTFCameraPerspective Perspective;

        /// <summary>
        /// Specifies if the camera uses a perspective or orthographic projection.
        /// Based on this, either the camera's `perspective` or `orthographic` property
        /// will be defined.
        /// </summary>
        public GLTFCameraType Type;

        public static GLTFCamera Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var camera = new GLTFCamera();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "orthographic":
                        camera.Orthographic = GLTFCameraOrthographic.Deserialize(root, reader);
                        break;
	                case "perspective":
		                camera.Perspective = GLTFCameraPerspective.Deserialize(root, reader);
		                break;
					default:
		                camera.DefaultPropertyDeserializer(root, reader);
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
	public class GLTFCameraOrthographic : GLTFProperty
	{
        /// <summary>
        /// The floating-point horizontal magnification of the view.
        /// </summary>
        public double XMag;

        /// <summary>
        /// The floating-point vertical magnification of the view.
        /// </summary>
        public double YMag;

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// </summary>
        public double ZFar;

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        public double ZNear;

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
                        cameraOrthographic.XMag = reader.ReadAsDouble().Value;
                        break;
                    case "ymag":
                        cameraOrthographic.YMag = reader.ReadAsDouble().Value;
                        break;
                    case "zfar":
                        cameraOrthographic.ZFar = reader.ReadAsDouble().Value;
                        break;
                    case "znear":
                        cameraOrthographic.ZNear = reader.ReadAsDouble().Value;
                        break;
	                default:
		                cameraOrthographic.DefaultPropertyDeserializer(root, reader);
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
	public class GLTFCameraPerspective : GLTFProperty
    {
        /// <summary>
        /// The floating-point aspect ratio of the field of view.
        /// When this is undefined, the aspect ratio of the canvas is used.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double AspectRatio;

        /// <summary>
        /// The floating-point vertical field of view in radians.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double YFov;

        /// <summary>
        /// The floating-point distance to the far clipping plane. When defined,
        /// `zfar` must be greater than `znear`.
        /// If `zfar` is undefined, runtime must use infinite projection matrix.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double ZFar;

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// <minimum>0.0</minimum>
        /// </summary>
        public double ZNear;

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
                        cameraPerspective.AspectRatio = reader.ReadAsDouble().Value;
                        break;
                    case "yfov":
                        cameraPerspective.YFov = reader.ReadAsDouble().Value;
                        break;
                    case "zfar":
                        cameraPerspective.ZFar = reader.ReadAsDouble().Value;
                        break;
                    case "znear":
                        cameraPerspective.ZNear = reader.ReadAsDouble().Value;
                        break;
	                default:
		                cameraPerspective.DefaultPropertyDeserializer(root, reader);
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
