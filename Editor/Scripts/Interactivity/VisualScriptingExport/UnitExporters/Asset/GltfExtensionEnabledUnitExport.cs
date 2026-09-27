using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GltfExtensionEnabledUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public Type unitType { get => typeof(GltfExtensionEnabledUnit); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GltfExtensionEnabledUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfExtensionEnabledUnit;

            AssetHelpers.GetExtensionEnabled(unitExporter, unit.extensionName, out var enabledRef);
            enabledRef.MapToPort(unit.enabled);

            return true;
        }

        public UnitLogs GetFeedback(IUnit unit)
        {
            var unitLog = new UnitLogs();

            var extensionUnit = unit as GltfExtensionEnabledUnit;
            if (string.IsNullOrEmpty(extensionUnit.extensionName))
                unitLog.errors.Add("No extension name set. Enter the glTF extension identifier to query, e.g. KHR_node_visibility.");

            return unitLog;
        }
    }
}
