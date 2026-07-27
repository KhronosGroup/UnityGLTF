using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GltfRuntimeLimitsUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfRuntimeLimitsUnit); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GltfRuntimeLimitsUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfRuntimeLimitsUnit;

            AssetHelpers.GetRuntimeLimit(unitExporter, AssetHelpers.LimitMaxActiveAnimationsPointer, out var animationsRef);
            AssetHelpers.GetRuntimeLimit(unitExporter, AssetHelpers.LimitMaxActiveDelaysPointer, out var delaysRef);
            AssetHelpers.GetRuntimeLimit(unitExporter, AssetHelpers.LimitMaxActivePropertyInterpolationsPointer, out var propInterpRef);
            AssetHelpers.GetRuntimeLimit(unitExporter, AssetHelpers.LimitMaxActiveVariableInterpolationsPointer, out var varInterpRef);

            animationsRef.MapToPort(unit.maxActiveAnimations);
            delaysRef.MapToPort(unit.maxActiveDelays);
            propInterpRef.MapToPort(unit.maxActivePropertyInterpolations);
            varInterpRef.MapToPort(unit.maxActiveVariableInterpolations);

            return true;
        }
    }
}
