using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct TransmissionPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("transmissionTexture");
        private static readonly int rotationHash = Shader.PropertyToID("transmissionTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("transmissionTextureTexCoord");
        private static readonly int transmissionFactorHash = Shader.PropertyToID("transmissionFactor");

        public TransformPointers transformPointers;
        public Pointer<float> transmissionFactor;

        public TransmissionPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            transmissionFactor = PointerHelpers.CreateFloatPointer(mat, transmissionFactorHash);
        }

        public static IPointer ProcessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_transmission
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("transmissionFactor") => matPointer.transmissionPointers.transmissionFactor,
                var a when a.Is("transmissionTexture") => ProcessExtensionsPointer(reader, matPointer.transmissionPointers),

                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, TransmissionPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_transmission/transmissionTexture/extensions/KHR_texture_transform/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("offset") => pointers.transformPointers.offset,
                var a when a.Is("rotation") => pointers.transformPointers.rotation,
                var a when a.Is("scale") => pointers.transformPointers.scale,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }
    }
}