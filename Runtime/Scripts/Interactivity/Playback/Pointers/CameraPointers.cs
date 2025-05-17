using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct CameraPointers
    {
        public Pointer<float> orthographicXMag;
        public Pointer<float> orthographicYMag;

        public Pointer<float> perspectiveAspectRatio;
        public Pointer<float> perspectiveYFov;

        public Pointer<float> zFar;
        public Pointer<float> zNear;

        public CameraPointers(in CameraData data)
        {
            var cam = data.unityCamera;
            // Unity does not allow you to set the width of the orthographic window directly.
            // cam.orthographicSize is the YMag and the width is then that value multiplied by your aspect ratio.
            orthographicXMag = new Pointer<float>()
            {
                setter = (v) => cam.orthographicSize = v / cam.aspect,
                getter = () => cam.orthographicSize * cam.aspect,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            orthographicYMag = new Pointer<float>()
            {
                setter = (v) => cam.orthographicSize = v,
                getter = () => cam.orthographicSize,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            perspectiveAspectRatio = new Pointer<float>()
            {
                setter = (v) => cam.aspect = v,
                getter = () => cam.aspect,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            perspectiveYFov = new Pointer<float>()
            {
                setter = (v) => cam.fieldOfView = v,
                getter = () => cam.fieldOfView,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            zNear = new Pointer<float>()
            {
                setter = (v) => cam.nearClipPlane = v,
                getter = () => cam.nearClipPlane,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            zFar = new Pointer<float>()
            {
                setter = (v) => cam.farClipPlane = v,
                getter = () => cam.farClipPlane,
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };
        }

        public static IPointer ProcessCameraPointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<CameraPointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            var nodeIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            var pointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /cameras/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("orthographic") => ProcessOrthographicPointer(reader, pointer),
                var a when a.Is("perspective") => ProcessPerspectivePointer(reader, pointer),
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessPerspectivePointer(StringSpanReader reader, CameraPointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /cameras/{}/perspective
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("aspectRatio") => pointer.perspectiveAspectRatio,
                var a when a.Is("yfov") => pointer.perspectiveYFov,
                var a when a.Is("zfar") => pointer.zFar,
                var a when a.Is("znear") => pointer.zNear,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessOrthographicPointer(StringSpanReader reader, CameraPointers pointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /cameras/{}/orthographic
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("xmag") => pointer.orthographicXMag,
                var a when a.Is("ymag") => pointer.orthographicYMag,
                var a when a.Is("zfar") => pointer.zFar,
                var a when a.Is("znear") => pointer.zNear,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }
    }
}