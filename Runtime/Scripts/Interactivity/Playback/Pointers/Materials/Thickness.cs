using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct ThicknessPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("thicknessTexture");
        private static readonly int rotationHash = Shader.PropertyToID("thicknessTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("thicknessTextureTexCoord");
        private static readonly int thicknessFactorHash = Shader.PropertyToID("thicknessFactor");

        public TransformPointers transformPointers;
        public Pointer<float> thicknessFactor;

        public ThicknessPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            thicknessFactor = PointerHelpers.CreateFloatPointer(mat, thicknessFactorHash);
        }

        public static IPointer ProcessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_volume
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("thicknessFactor") => matPointer.thicknessPointers.thicknessFactor,
                var a when a.Is("attenuationDistance") => matPointer.attenuationDistance,
                var a when a.Is("attenuationColor") => matPointer.attenuationColor,
                var a when a.Is("thicknessTexture") => ProcessExtensionsPointer(reader, matPointer.thicknessPointers),

                _ => PointerHelpers.InvalidPointer(),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, ThicknessPointers pointers)
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
                _ => PointerHelpers.InvalidPointer(),
            };
        }
    }
}