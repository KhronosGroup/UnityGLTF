using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Read-only "active camera" state under /extensions/KHR_interactivity/activeCamera.
    /// Values are NaN when no camera is available or the projection type does not match.
    /// </summary>
    public struct ActiveCameraPointers
    {
        public ReadOnlyPointer<float3> position;
        public ReadOnlyPointer<float4> rotation;
        public ReadOnlyPointer<float> perspectiveAspectRatio;
        public ReadOnlyPointer<float> perspectiveYFov;
        public ReadOnlyPointer<float> perspectiveZNear;
        public ReadOnlyPointer<float> perspectiveZFar;
        public ReadOnlyPointer<float> orthographicXMag;
        public ReadOnlyPointer<float> orthographicYMag;
        public ReadOnlyPointer<float> orthographicZNear;
        public ReadOnlyPointer<float> orthographicZFar;

        public static ActiveCameraPointers CreatePointers()
        {
            // Unity is left-handed and glTF is right-handed. Flipping Z also maps Unity's +Z camera forward
            // onto glTF's -Z camera forward, so the identity rotation matches the glTF camera orientation.
            return new ActiveCameraPointers
            {
                position = new ReadOnlyPointer<float3>(() =>
                {
                    var cam = Camera.main;
                    return cam == null ? new float3(float.NaN) : ((float3)cam.transform.position).SwapHandedness();
                }),
                rotation = new ReadOnlyPointer<float4>(() =>
                {
                    var cam = Camera.main;
                    return cam == null ? new float4(float.NaN) : cam.transform.rotation.SwapHandedness().ToFloat4();
                }),
                perspectiveAspectRatio = Perspective(cam => cam.aspect),
                perspectiveYFov = Perspective(cam => cam.fieldOfView * Mathf.Deg2Rad),
                perspectiveZNear = Perspective(cam => cam.nearClipPlane),
                perspectiveZFar = Perspective(cam => cam.farClipPlane),
                orthographicXMag = Orthographic(cam => cam.orthographicSize * cam.aspect),
                orthographicYMag = Orthographic(cam => cam.orthographicSize),
                orthographicZNear = Orthographic(cam => cam.nearClipPlane),
                orthographicZFar = Orthographic(cam => cam.farClipPlane),
            };
        }

        private static ReadOnlyPointer<float> Perspective(System.Func<Camera, float> getter)
        {
            return new ReadOnlyPointer<float>(() =>
            {
                var cam = Camera.main;
                return cam == null || cam.orthographic ? float.NaN : getter(cam);
            });
        }

        private static ReadOnlyPointer<float> Orthographic(System.Func<Camera, float> getter)
        {
            return new ReadOnlyPointer<float>(() =>
            {
                var cam = Camera.main;
                return cam == null || !cam.orthographic ? float.NaN : getter(cam);
            });
        }

        public IPointer ProcessActiveCameraPointer(StringSpanReader reader)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /extensions/KHR_interactivity/activeCamera/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("position") => position,
                var a when a.Is("rotation") => rotation,
                var a when a.Is("perspective") => ProcessPerspective(reader),
                var a when a.Is("orthographic") => ProcessOrthographic(reader),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private IPointer ProcessPerspective(StringSpanReader reader)
        {
            reader.AdvanceToNextToken('/');

            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("aspectRatio") => perspectiveAspectRatio,
                var a when a.Is("yfov") => perspectiveYFov,
                var a when a.Is("znear") => perspectiveZNear,
                var a when a.Is("zfar") => perspectiveZFar,
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private IPointer ProcessOrthographic(StringSpanReader reader)
        {
            reader.AdvanceToNextToken('/');

            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("xmag") => orthographicXMag,
                var a when a.Is("ymag") => orthographicYMag,
                var a when a.Is("znear") => orthographicZNear,
                var a when a.Is("zfar") => orthographicZFar,
                _ => PointerHelpers.InvalidPointer(),
            };
        }
    }
}
