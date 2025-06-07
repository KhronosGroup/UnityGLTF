using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct EmissivePointers
    {
        private static readonly int textureHash = Shader.PropertyToID("emissiveTexture");
        private static readonly int rotationHash = Shader.PropertyToID("emissiveTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("emissiveTextureTexCoord");
        private static readonly int emissiveFactorHash = Shader.PropertyToID("emissiveFactor");

        public TransformPointers transformPointers;
        public Pointer<Color3> emissiveFactor;

        public EmissivePointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            emissiveFactor = PointerHelpers.CreateColorRGBPointer(mat, emissiveFactorHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, EmissivePointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/emissiveTexture/extensions/KHR_texture_transform/
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