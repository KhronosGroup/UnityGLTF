using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct SheenRoughnessPointers
    {
        private static readonly int rotationHash = Shader.PropertyToID("sheenRoughnessTextureRotation");
        private static readonly int textureHash = Shader.PropertyToID("sheenRoughnessTexture");
        private static readonly int texCoordHash = Shader.PropertyToID("sheenRoughnessTextureTexCoord");
        private static readonly int factorHash = Shader.PropertyToID("sheenRoughnessFactor");

        public TransformPointers transformPointers;
        public Pointer<float> factor;

        public SheenRoughnessPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            factor = PointerHelpers.CreateFloatPointer(mat, factorHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, SheenRoughnessPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_iridescence/iridescenceTexture/extensions/KHR_texture_transform/
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