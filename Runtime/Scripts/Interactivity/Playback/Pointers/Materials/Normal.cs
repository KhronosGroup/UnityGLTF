using System;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct NormalPointers
    {
        private static readonly int textureHash = Shader.PropertyToID("normalTexture");
        private static readonly int rotationHash = Shader.PropertyToID("normalTextureRotation");
        private static readonly int texCoordHash = Shader.PropertyToID("normalTextureTexCoord");
        private static readonly int normalScaleHash = Shader.PropertyToID("normalScale");

        public TransformPointers transformPointers;
        public Pointer<float> normalScale;

        public NormalPointers(Material mat)
        {
            transformPointers = new TransformPointers()
            {
                offset = PointerHelpers.CreateOffsetPointer(mat, textureHash),
                scale = PointerHelpers.CreateScalePointer(mat, textureHash),
                rotation = PointerHelpers.CreateFloatPointer(mat, rotationHash),
                texCoord = PointerHelpers.CreateFloatPointer(mat, texCoordHash)
            };

            normalScale = PointerHelpers.CreateFloatPointer(mat, normalScaleHash);
        }

        public static IPointer ProcessExtensionsPointer(StringSpanReader reader, NormalPointers pointers)
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
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }
    }
}