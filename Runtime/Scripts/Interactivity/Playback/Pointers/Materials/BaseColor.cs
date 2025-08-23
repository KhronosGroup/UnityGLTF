using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct BaseColorPointers
    {
        private static readonly int baseColorFactorHash = Shader.PropertyToID("baseColorFactor");
        private static readonly int rotationHash = Shader.PropertyToID("baseColorTextureRotation");
        private static readonly int textureHash = Shader.PropertyToID("baseColorTexture");
        private static readonly int texCoordHash = Shader.PropertyToID("baseColorTextureTexCoord");

        public TransformPointers transformPointers;
        public Pointer<Color> baseColorFactor;

        public BaseColorPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            baseColorFactor = PointerHelpers.CreateColorRGBAPointer(mat, baseColorFactorHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, BaseColorPointers pointers)
        {
            // TODO: These come in the form of baseColorTexture/extensions/KHR_texture_transform/{PROPERTY}
            // We're skipping ahead to get there with this triple-call.
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/pbrMetallicRoughness/baseColorTexture/extensions/KHR_texture_transform/
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