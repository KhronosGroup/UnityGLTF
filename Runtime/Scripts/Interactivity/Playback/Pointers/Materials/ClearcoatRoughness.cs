using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct ClearcoatRoughnessPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("clearcoatRoughnessTexture");
        private static readonly int rotationHash = Shader.PropertyToID("clearcoatRoughnessTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("clearcoatRoughnessTextureTexCoord");
        private static readonly int factorHash = Shader.PropertyToID("clearcoatRoughnessFactor");

        public TransformPointers transformPointers;
        public Pointer<float> factor;

        public ClearcoatRoughnessPointers(Material mat)
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

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, ClearcoatRoughnessPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_clearcoat/clearcoatRoughnessTexture/extensions/KHR_texture_transform/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("offset") => pointers.transformPointers.offset,
                var a when a.Is("rotation") => pointers.transformPointers.rotation,
                var a when a.Is("scale") => pointers.transformPointers.scale,
                _ => PointerHelpers.InvalidPointer(),
            };
        }
    }
}