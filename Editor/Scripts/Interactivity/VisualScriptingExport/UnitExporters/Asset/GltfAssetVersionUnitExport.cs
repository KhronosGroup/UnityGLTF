using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GltfAssetVersionUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfAssetVersionUnit); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GltfAssetVersionUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfAssetVersionUnit;

            AssetHelpers.GetAssetMajorVersion(unitExporter, out var majorRef);
            AssetHelpers.GetAssetMinorVersion(unitExporter, out var minorRef);

            majorRef.MapToPort(unit.majorVersion);
            minorRef.MapToPort(unit.minorVersion);

            return true;
        }
    }
}
