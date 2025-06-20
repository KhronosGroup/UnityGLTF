using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct IridescencePointers
    {
        public static readonly int textureHash = Shader.PropertyToID("iridescenceTexture");
        public static readonly int rotationHash = Shader.PropertyToID("iridescenceTextureRotation");
        public static readonly int texCoordHash = Shader.PropertyToID("iridescenceTextureTexCoord");
        public static readonly int iridescenceFactorHash = Shader.PropertyToID("iridescenceFactor");
        public static readonly int iridescenceIorHash = Shader.PropertyToID("iridescenceIor");

        public TransformPointers transformPointers;
        public Pointer<float> iridescenceFactor;
        public Pointer<float> iridescenceIor;

        public IridescencePointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            iridescenceFactor = PointerHelpers.CreateFloatPointer(mat, iridescenceFactorHash);
            iridescenceIor = PointerHelpers.CreateFloatPointer(mat, iridescenceIorHash);
        }

        public static IPointer ProcessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/KHR_materials_iridescence
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("iridescenceFactor") => matPointer.iridescencePointers.iridescenceFactor,
                var a when a.Is("iridescenceIor") => matPointer.iridescencePointers.iridescenceIor,
                var a when a.Is("iridescenceThicknessMinimum") => matPointer.iridescenceThicknessPointers.min,
                var a when a.Is("iridescenceThicknessMaximum") => matPointer.iridescenceThicknessPointers.max,
                var a when a.Is("iridescenceTexture") => ProcessExtensionsPointer(reader, matPointer.iridescencePointers),
                var a when a.Is("iridescenceThicknessTexture") => IridescenceThicknessPointers.ProcessExtensionsPointer(reader, matPointer.iridescenceThicknessPointers),

                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, IridescencePointers pointers)
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