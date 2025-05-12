using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct ClearcoatPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("clearcoatTexture");
        private static readonly int rotationHash = Shader.PropertyToID("clearcoatTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("clearcoatTextureTexCoord");
        private static readonly int factorHash = Shader.PropertyToID("clearcoatFactor");

        public TransformPointers transformPointers;
        public Pointer<float> factor;

        public ClearcoatPointers(Material mat)
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

        public static IPointer ProcessClearcoatPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_clearcoat
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("clearcoatFactor") => matPointer.clearcoatPointers.factor,
                var a when a.Is("clearcoatRoughnessFactor") => matPointer.clearcoatRoughnessPointers.factor,
                var a when a.Is("clearcoatTexture") => ProcessExtensionsPointer(reader, matPointer.clearcoatPointers),
                var a when a.Is("clearcoatRoughnessTexture") => ClearcoatRoughnessPointers.ProcessExtensionsPointer(reader, matPointer.clearcoatRoughnessPointers),

                // TODO: This property is not mentioned anywhere in the PBRGraph UnityGLTF shader so I didn't include it.
                //var a when a.Is("clearcoatNormalTexture") => ,

                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, ClearcoatPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_clearcoat/clearcoatTexture/extensions/KHR_texture_transform/
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