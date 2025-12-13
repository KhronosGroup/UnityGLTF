using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct MetallicRoughnessPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("metallicRoughnessTexture");
        private static readonly int rotationHash = Shader.PropertyToID("metallicRoughnessTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("metallicRoughnessTextureTexCoord");
        private static readonly int metallicFactorHash = Shader.PropertyToID("metallicFactor");
        private static readonly int roughnessFactorHash = Shader.PropertyToID("roughnessFactor");

        public TransformPointers transformPointers;
        public Pointer<float> metallicFactor;
        public Pointer<float> roughnessFactor;

        public MetallicRoughnessPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            metallicFactor = PointerHelpers.CreateFloatPointer(mat, metallicFactorHash);
            roughnessFactor = PointerHelpers.CreateFloatPointer(mat, roughnessFactorHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, MetallicRoughnessPointers pointers)
        {
            // TODO: These come in the form of baseColorTexture/extensions/KHR_texture_transform/{PROPERTY}
            // We're skipping ahead to get there with this triple-call.
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/pbrMetallicRoughness/metallicRoughnessTexture/extensions/KHR_texture_transform/
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