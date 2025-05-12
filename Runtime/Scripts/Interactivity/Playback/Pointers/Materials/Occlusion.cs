using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct OcclusionPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("occlusionTexture");
        private static readonly int rotationHash = Shader.PropertyToID("occlusionTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("occlusionTextureTexCoord");
        private static readonly int occlusionStrengthHash = Shader.PropertyToID("occlusionStrength");

        public TransformPointers transformPointers;
        public Pointer<float> occlusionStrength;

        public OcclusionPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            occlusionStrength = PointerHelpers.CreateFloatPointer(mat, occlusionStrengthHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, OcclusionPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/occlusionTexture/extensions/KHR_texture_transform/
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