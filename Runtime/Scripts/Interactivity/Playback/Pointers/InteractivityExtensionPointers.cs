using System;
using System.Collections.Generic;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Virtual Object Model properties defined by KHR_interactivity under /extensions/KHR_interactivity.
    /// </summary>
    public static class InteractivityExtensionPointers
    {
        public const int SUPPORTED_MAJOR_VERSION = 2;
        public const int SUPPORTED_MINOR_VERSION = 0;

        /// <summary>
        /// Runtime limits exposed to graphs. This implementation has no explicit limits,
        /// which the spec allows to be reported as int.MaxValue.
        /// </summary>
        public const int MAX_ACTIVE_ANIMATIONS = int.MaxValue;
        public const int MAX_ACTIVE_DELAYS = int.MaxValue;
        public const int MAX_ACTIVE_PROPERTY_INTERPOLATIONS = int.MaxValue;
        public const int MAX_ACTIVE_VARIABLE_INTERPOLATIONS = int.MaxValue;

        /// <summary>
        /// Extensions this runtime supports. An extension listed in extensionsUsed gets an
        /// asset/extensions/NAME/enabled property only when it is in this set.
        /// </summary>
        public static readonly HashSet<string> SupportedExtensions = new()
        {
            "KHR_interactivity",
            "KHR_node_visibility",
            "KHR_node_selectability",
            "KHR_node_hoverability",
            "KHR_animation_pointer",
            "KHR_texture_transform",
            "KHR_materials_clearcoat",
            "KHR_materials_dispersion",
            "KHR_materials_emissive_strength",
            "KHR_materials_ior",
            "KHR_materials_iridescence",
            "KHR_materials_sheen",
            "KHR_materials_specular",
            "KHR_materials_transmission",
            "KHR_materials_unlit",
            "KHR_materials_volume",
            "KHR_mesh_quantization",
            "KHR_lights_punctual",
            "KHR_texture_basisu",
            "KHR_draco_mesh_compression",
            "EXT_meshopt_compression",
            "EXT_mesh_gpu_instancing",
        };

        private static readonly ReadOnlyPointer<bool> _enabled = new(() => true);

        public static IPointer Process(StringSpanReader reader, BehaviourEngine engine, ActiveCameraPointers activeCamera, SceneData sceneData)
        {
            reader.AdvanceToNextToken('/');

            if (!reader.AsReadOnlySpan().Is(InteractivityGraphExtension.EXTENSION_NAME))
                return PointerHelpers.InvalidPointer();

            reader.AdvanceToNextToken('/');

            // Path so far: /extensions/KHR_interactivity/
            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("activeCamera") => activeCamera.ProcessActiveCameraPointer(reader),
                var a when a.Is("asset") => ProcessAsset(reader, sceneData),
                var a when a.Is("limits") => ProcessLimits(reader),
                var a when a.Is("delays") => ProcessDelay(reader, engine),
                var a when a.Is("events") => ProcessEvent(reader, engine),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessAsset(StringSpanReader reader, SceneData sceneData)
        {
            reader.AdvanceToNextToken('/');
            ParseAssetVersion(sceneData.assetVersion, out var major, out var minor);

            // The presented version is the minimum of the asset version and the supported version.
            if (major > SUPPORTED_MAJOR_VERSION || (major == SUPPORTED_MAJOR_VERSION && minor > SUPPORTED_MINOR_VERSION))
            {
                major = SUPPORTED_MAJOR_VERSION;
                minor = SUPPORTED_MINOR_VERSION;
            }

            switch (reader.AsReadOnlySpan())
            {
                case var a when a.Is("majorVersion"):
                    return new ReadOnlyPointer<int>(() => major);
                case var a when a.Is("minorVersion"):
                    return new ReadOnlyPointer<int>(() => minor);
                case var a when a.Is(Pointers.EXTENSIONS):
                    reader.AdvanceToNextToken('/');
                    var extensionName = reader.ToString();
                    reader.AdvanceToNextToken('/');

                    if (!reader.AsReadOnlySpan().Is("enabled"))
                        return PointerHelpers.InvalidPointer();

                    var used = sceneData.extensionsUsed != null && sceneData.extensionsUsed.Contains(extensionName);
                    return used && SupportedExtensions.Contains(extensionName) ? _enabled : PointerHelpers.InvalidPointer();
                default:
                    return PointerHelpers.InvalidPointer();
            }
        }

        private static void ParseAssetVersion(string version, out int major, out int minor)
        {
            major = SUPPORTED_MAJOR_VERSION;
            minor = SUPPORTED_MINOR_VERSION;

            if (string.IsNullOrEmpty(version))
                return;

            var parts = version.Split('.');

            if (parts.Length == 2 && int.TryParse(parts[0], out var ma) && int.TryParse(parts[1], out var mi))
            {
                major = ma;
                minor = mi;
            }
        }

        private static IPointer ProcessLimits(StringSpanReader reader)
        {
            reader.AdvanceToNextToken('/');

            return reader.AsReadOnlySpan() switch
            {
                var a when a.Is("maxActiveAnimations") => new ReadOnlyPointer<int>(() => MAX_ACTIVE_ANIMATIONS),
                var a when a.Is("maxActiveDelays") => new ReadOnlyPointer<int>(() => MAX_ACTIVE_DELAYS),
                var a when a.Is("maxActivePropertyInterpolations") => new ReadOnlyPointer<int>(() => MAX_ACTIVE_PROPERTY_INTERPOLATIONS),
                var a when a.Is("maxActiveVariableInterpolations") => new ReadOnlyPointer<int>(() => MAX_ACTIVE_VARIABLE_INTERPOLATIONS),
                _ => PointerHelpers.InvalidPointer(),
            };
        }

        private static IPointer ProcessDelay(StringSpanReader reader, BehaviourEngine engine)
        {
            reader.AdvanceToNextToken('/');

            if (engine == null || !PointerTemplate.TryParseReferenceSegment(reader.AsReadOnlySpan(), out var r) || r.kind != RefKind.Delay)
                return PointerHelpers.InvalidPointer();

            return engine.nodeDelayManager.IsActive(r) ? new ReadOnlyPointer<Ref>(() => r) : PointerHelpers.InvalidPointer();
        }

        private static IPointer ProcessEvent(StringSpanReader reader, BehaviourEngine engine)
        {
            reader.AdvanceToNextToken('/');

            if (engine == null || !PointerTemplate.TryParseReferenceSegment(reader.AsReadOnlySpan(), out var r) || r.kind != RefKind.Event)
                return PointerHelpers.InvalidPointer();

            return engine.IsEventReference(r) ? new ReadOnlyPointer<Ref>(() => r) : PointerHelpers.InvalidPointer();
        }
    }
}
