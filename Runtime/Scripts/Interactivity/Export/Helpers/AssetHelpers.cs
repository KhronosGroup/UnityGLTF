using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    /// <summary>
    /// Helpers to read the KHR_interactivity read-only "Asset Capabilities" (spec §4.2.1) and
    /// "Implementation-Specific Runtime Limits" (spec §4.2.2) virtual pointers via pointer/get.
    /// These let a behavior graph branch on the glTF version, on whether a given extension is
    /// supported, or on the implementation's runtime limits.
    /// </summary>
    public static class AssetHelpers
    {
        // Asset Capabilities (§4.2.1)
        public const string AssetMajorVersionPointer = "/extensions/KHR_interactivity/asset/majorVersion";
        public const string AssetMinorVersionPointer = "/extensions/KHR_interactivity/asset/minorVersion";
        public const string AssetExtensionEnabledPointerFormat = "/extensions/KHR_interactivity/asset/extensions/{0}/enabled";

        // Implementation-Specific Runtime Limits (§4.2.2)
        public const string LimitMaxActiveAnimationsPointer = "/extensions/KHR_interactivity/limits/maxActiveAnimations";
        public const string LimitMaxActiveDelaysPointer = "/extensions/KHR_interactivity/limits/maxActiveDelays";
        public const string LimitMaxActivePropertyInterpolationsPointer = "/extensions/KHR_interactivity/limits/maxActivePropertyInterpolations";
        public const string LimitMaxActiveVariableInterpolationsPointer = "/extensions/KHR_interactivity/limits/maxActiveVariableInterpolations";

        /// <summary>Reads the major version component of the glTF spec used for presenting the asset.</summary>
        public static void GetAssetMajorVersion(INodeExporter exporter, out ValueOutRef value)
        {
            GetIntPointer(exporter, AssetMajorVersionPointer, out value);
        }

        /// <summary>Reads the minor version component of the glTF spec used for presenting the asset.</summary>
        public static void GetAssetMinorVersion(INodeExporter exporter, out ValueOutRef value)
        {
            GetIntPointer(exporter, AssetMinorVersionPointer, out value);
        }

        /// <summary>
        /// Reads whether the given glTF extension is both listed in the asset's extensionsUsed and
        /// supported by the implementation. Reads false for unsupported/unknown extensions.
        /// </summary>
        public static void GetExtensionEnabled(INodeExporter exporter, string extensionName, out ValueOutRef value)
        {
            var pointer = string.Format(AssetExtensionEnabledPointerFormat, extensionName);
            var getNode = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.AddPointerConfig(getNode, pointer, GltfTypes.Bool);
            value = getNode.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }

        /// <summary>
        /// Reads one of the implementation-specific runtime limits. Pass one of the Limit*Pointer
        /// constants defined on this class.
        /// </summary>
        public static void GetRuntimeLimit(INodeExporter exporter, string limitPointer, out ValueOutRef value)
        {
            GetIntPointer(exporter, limitPointer, out value);
        }

        private static void GetIntPointer(INodeExporter exporter, string pointer, out ValueOutRef value)
        {
            var getNode = exporter.CreateNode<Pointer_GetNode>();
            PointersHelper.AddPointerConfig(getNode, pointer, GltfTypes.Int);
            value = getNode.FirstValueOut().ExpectedType(ExpectedType.Int);
        }
    }
}
