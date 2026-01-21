using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct IridescenceThicknessPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("iridescenceThicknessTexture");
        private static readonly int rotationHash = Shader.PropertyToID("iridescenceThicknessTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("iridescenceThicknessTextureTexCoord");
        private static readonly int minHash = Shader.PropertyToID("iridescenceThicknessMinimum");
        private static readonly int maxHash = Shader.PropertyToID("iridescenceThicknessMaximum");

        public TransformPointers transformPointers;
        public Pointer<float> min;
        public Pointer<float> max;

        public IridescenceThicknessPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            min = PointerHelpers.CreateFloatPointer(mat, minHash);
            max = PointerHelpers.CreateFloatPointer(mat, maxHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, IridescenceThicknessPointers pointers)
        {
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_iridescence/iridescenceThicknessTexture/extensions/KHR_texture_transform/
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