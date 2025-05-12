using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback.Materials
{
    public struct TransformPointers
    {
        public Pointer<float2> offset;
        public Pointer<float2> scale;
        public Pointer<float> rotation;
        public Pointer<float> texCoord;
    }

    public struct MaterialPointers
    {
        public static readonly int alphaCutoffHash = Shader.PropertyToID("alphaCutoff");
        public static readonly int iorHash = Shader.PropertyToID("ior");
        public static readonly int attenuationDistanceHash = Shader.PropertyToID("attenuationDistance");
        public static readonly int attenuationColorHash = Shader.PropertyToID("attenuationColor");
        public static readonly int dispersionHash = Shader.PropertyToID("dispersion");

        public Pointer<float> alphaCutoff;
        public Pointer<float> ior;
        public Pointer<float> attenuationDistance;
        public Pointer<Color3> attenuationColor;
        public Pointer<float> dispersion;

        public BaseColorPointers baseColorPointers;
        public ClearcoatPointers clearcoatPointers;
        public ClearcoatRoughnessPointers clearcoatRoughnessPointers;
        public EmissivePointers emissivePointers;
        public IridescencePointers iridescencePointers;
        public IridescenceThicknessPointers iridescenceThicknessPointers;
        public MetallicRoughnessPointers metallicRoughnessPointers;
        public NormalPointers normalPointers;
        public OcclusionPointers occlusionPointers;
        public SheenPointers sheenPointers;
        public SheenRoughnessPointers sheenRoughnessPointers;
        public SpecularPointers specularPointers;
        public SpecularColorPointers specularColorPointers;
        public ThicknessPointers thicknessPointers;
        public TransmissionPointers transmissionPointers;

        public Material material { get; private set; }

        public MaterialPointers(Material mat)
        {
            material = mat;

            alphaCutoff = PointerHelpers.CreateFloatPointer(mat, alphaCutoffHash);
            ior = PointerHelpers.CreateFloatPointer(mat, iorHash);
            attenuationDistance = PointerHelpers.CreateFloatPointer(mat, attenuationDistanceHash);
            attenuationColor = PointerHelpers.CreateColorRGBPointer(mat, attenuationColorHash);
            dispersion = PointerHelpers.CreateFloatPointer(mat, dispersionHash);

            baseColorPointers = new(mat);
            clearcoatPointers = new(mat);
            clearcoatRoughnessPointers = new(mat);
            emissivePointers = new(mat);
            iridescencePointers = new(mat);
            iridescenceThicknessPointers = new(mat);
            metallicRoughnessPointers = new(mat);
            normalPointers = new(mat);
            occlusionPointers = new(mat);
            sheenPointers = new(mat);
            sheenRoughnessPointers = new(mat);
            specularPointers = new(mat);
            specularColorPointers = new(mat);
            thicknessPointers = new(mat);
            transmissionPointers = new(mat);
        }

        public static IPointer ProcessMaterialPointer(StringSpanReader reader, BehaviourEngineNode engineNode, List<MaterialPointers> pointers)
        {
            reader.AdvanceToNextToken('/');

            var nodeIndex = PointerResolver.GetIndexFromArgument(reader, engineNode);

            var pointer = pointers[nodeIndex];

            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("alphaCutoff") => pointer.alphaCutoff,
                var a when a.Is("emissiveFactor") => pointer.emissivePointers.emissiveFactor,
                var a when a.Is("normalTexture") => ProcessNormalMapPointer(reader, pointer),
                var a when a.Is("occlusionTexture") => ProcessOcclusionMapPointer(reader, pointer),
                var a when a.Is("emissiveTexture") => ProcessEmissiveMapPointer(reader, pointer),
                var a when a.Is("pbrMetallicRoughness") => ProcessPBRMetallicRoughnessPointer(reader, pointer),
                var a when a.Is("extensions") => ProcessExtensionPointer(reader, pointer),
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessPBRMetallicRoughnessPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/pbrMetallicRoughness/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("baseColorFactor") => matPointer.baseColorPointers.baseColorFactor,
                var a when a.Is("baseColorTexture") => BaseColorPointers.ProcessExtensionsPointer(reader, matPointer.baseColorPointers),
                var a when a.Is("metallicRoughnessTexture") => MetallicRoughnessPointers.ProcessExtensionsPointer(reader, matPointer.metallicRoughnessPointers),
                var a when a.Is("metallicFactor") => matPointer.metallicRoughnessPointers.metallicFactor,
                var a when a.Is("roughnessFactor") => matPointer.metallicRoughnessPointers.roughnessFactor,
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessOcclusionMapPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/occlusionTexture/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("strength") => matPointer.occlusionPointers.occlusionStrength,
                var a when a.Is("extensions") => OcclusionPointers.ProcessExtensionsPointer(reader, matPointer.occlusionPointers),
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessNormalMapPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/normalTexture/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("scale") => matPointer.normalPointers.normalScale,
                var a when a.Is("extensions") => NormalPointers.ProcessExtensionsPointer(reader, matPointer.normalPointers),
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessEmissiveMapPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/emissiveTexture/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("extensions") => EmissivePointers.ProcessExtensionsPointer(reader, matPointer.emissivePointers),
                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }

        private static IPointer ProcessExtensionPointer(StringSpanReader reader, MaterialPointers matPointer)
        {
            reader.AdvanceToNextToken('/');

            // Path so far: /materials/{}/extensions/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("KHR_materials_clearcoat") => ClearcoatPointers.ProcessClearcoatPointer(reader, matPointer),
                var a when a.Is("KHR_materials_dispersion") => matPointer.dispersion,
                var a when a.Is("KHR_materials_ior") => matPointer.ior,
                var a when a.Is("KHR_materials_iridescence") => IridescencePointers.ProcessPointer(reader, matPointer),
                var a when a.Is("KHR_materials_sheen") => SheenPointers.ProcessPointer(reader, matPointer),
                var a when a.Is("KHR_materials_specular") => SpecularPointers.ProcessPointer(reader, matPointer),
                var a when a.Is("KHR_materials_transmission") => TransmissionPointers.ProcessPointer(reader, matPointer),
                var a when a.Is("KHR_materials_volume") => ThicknessPointers.ProcessPointer(reader, matPointer),

                _ => throw new InvalidOperationException($"Property {reader.ToString()} is unsupported at this time!"),
            };
        }
    }
}