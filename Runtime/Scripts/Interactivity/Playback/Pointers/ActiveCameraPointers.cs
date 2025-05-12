using System;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public struct ActiveCameraPointers
    {
        // These are readonly in the spec but I'm a rebel
        public Pointer<float3> translation;
        public Pointer<quaternion> rotation;

        public static ActiveCameraPointers CreatePointers()
        {
            // Unity coordinate system differs from the GLTF one.
            // Unity is left-handed with y-up and z-forward.
            // GLTF is right-handed with y-up and z-forward.
            // Handedness is easiest to swap here though we could do it during deserialization for performance.
            var pointers = new ActiveCameraPointers();

            pointers.translation = new Pointer<float3>()
            {
                setter = (v) => Camera.main.transform.localPosition = v.SwapHandedness(),
                getter = () => Camera.main.transform.localPosition.SwapHandedness(),
                evaluator = (a, b, t) => math.lerp(a, b, t)
            };

            pointers.rotation = new Pointer<quaternion>()
            {
                setter = (v) => Camera.main.transform.localRotation = ((Quaternion)v).SwapHandedness(),
                getter = () => Camera.main.transform.localRotation.SwapHandedness(),
                evaluator = (a, b, t) => math.slerp(a, b, t)
            };

            return pointers;
        }

        public IPointer ProcessActiveCameraPointer(StringSpanReader reader)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /activeCamera/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("translation") => translation,
                var a when a.Is("rotation") => rotation,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }
    }
}