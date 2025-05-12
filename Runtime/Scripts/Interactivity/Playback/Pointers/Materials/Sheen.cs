using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct SheenPointers
    {
        private static readonly int rotationHash = Shader.PropertyToID("sheenColorTextureRotation");
        private static readonly int textureHash = Shader.PropertyToID("sheenColorTexture");
        private static readonly int texCoordHash = Shader.PropertyToID("sheenColorTextureTexCoord");
        private static readonly int colorFactorHash = Shader.PropertyToID("sheenColorFactor");

        public TransformPointers transformPointers;
        public Pointer<Color3> colorFactor;

        public SheenPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            colorFactor = PointerHelpers.CreateColorRGBPointer(mat, colorFactorHash);
        }

        public static IPointer ProcessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_sheen
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("sheenColorFactor") => matPointer.sheenPointers.colorFactor,
                var a when a.Is("sheenRoughnessFactor") => matPointer.sheenRoughnessPointers.factor,
                var a when a.Is("sheenColorTexture") => ProcessExtensionsPointer(reader, matPointer.sheenPointers),
                var a when a.Is("sheenRoughnessTexture") => SheenRoughnessPointers.ProcessExtensionsPointer(reader, matPointer.sheenRoughnessPointers),

                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, SheenPointers pointers)
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