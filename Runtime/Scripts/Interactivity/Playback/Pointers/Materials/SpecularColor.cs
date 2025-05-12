using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct SpecularColorPointers
    {
        private static readonly int rotationHash = Shader.PropertyToID("specularColorTextureRotation");
        private static readonly int textureHash = Shader.PropertyToID("specularColorTexture");
        private static readonly int texCoordHash = Shader.PropertyToID("specularColorTextureTexCoord");
        private static readonly int colorFactorHash = Shader.PropertyToID("specularColorFactor");

        public TransformPointers transformPointers;
        public Pointer<Color3> specularColorFactor;

        public SpecularColorPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            specularColorFactor = PointerHelpers.CreateColorRGBPointer(mat, colorFactorHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, SpecularColorPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_specular/specularColorTexture/extensions/KHR_texture_transform/
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