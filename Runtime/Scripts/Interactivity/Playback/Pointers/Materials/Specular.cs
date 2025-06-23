using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct SpecularPointers
    {
        private static readonly int rotationHash = Shader.PropertyToID("specularTextureRotation");
        private static readonly int textureHash = Shader.PropertyToID("specularTexture");
        private static readonly int texCoordHash = Shader.PropertyToID("specularTextureTexCoord");
        private static readonly int factorHash = Shader.PropertyToID("specularFactor");

        public TransformPointers transformPointers;
        public Pointer<float> specularFactor;

        public SpecularPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            specularFactor = PointerHelpers.CreateFloatPointer(mat, factorHash);
        }

        public static IPointer ProcessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_specular
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("specularFactor") => matPointer.specularPointers.specularFactor,
                var a when a.Is("specularColorFactor") => matPointer.specularColorPointers.specularColorFactor,
                var a when a.Is("specularTexture") => ProcessExtensionsPointer(reader, matPointer.specularPointers),
                var a when a.Is("specularColorTexture") => SpecularColorPointers.ProcessExtensionsPointer(reader, matPointer.specularColorPointers),

                _ => PointerHelpers.InvalidPointer(),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, SpecularPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_specular/specularTexture/extensions/KHR_texture_transform/
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