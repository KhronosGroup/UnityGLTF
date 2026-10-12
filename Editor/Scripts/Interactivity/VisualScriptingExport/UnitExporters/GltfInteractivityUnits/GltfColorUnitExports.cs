using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    // Exporters for the glTF color units in GltfColorUnits.cs, mapped to math/rgbToOkLCh and math/rgbFromOkLCh.

    public class GltfRgbToOkLChUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfRgbToOkLCh); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfRgbToOkLChUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfRgbToOkLCh;
            ColorFunctions.Extract(unitExporter, unit.color, out var r, out var g, out var b, out _);

            var node = unitExporter.CreateNode<Math_RgbToOkLChNode>();
            node.ValueIn(Math_RgbToOkLChNode.IdInputR).ConnectToSource(r).SetType(TypeRestriction.LimitToFloat);
            node.ValueIn(Math_RgbToOkLChNode.IdInputG).ConnectToSource(g).SetType(TypeRestriction.LimitToFloat);
            node.ValueIn(Math_RgbToOkLChNode.IdInputB).ConnectToSource(b).SetType(TypeRestriction.LimitToFloat);
            node.ValueOut(Math_RgbToOkLChNode.IdOutputL).ExpectedType(ExpectedType.Float).MapToPort(unit.l);
            node.ValueOut(Math_RgbToOkLChNode.IdOutputC).ExpectedType(ExpectedType.Float).MapToPort(unit.c);
            node.ValueOut(Math_RgbToOkLChNode.IdOutputH).ExpectedType(ExpectedType.Float).MapToPort(unit.h);
            return true;
        }
    }

    public class GltfRgbFromOkLChUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfRgbFromOkLCh); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfRgbFromOkLChUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfRgbFromOkLCh;

            var node = unitExporter.CreateNode<Math_RgbFromOkLChNode>();
            node.ValueIn(Math_RgbFromOkLChNode.IdInputL).MapToInputPort(unit.l).SetType(TypeRestriction.LimitToFloat);
            node.ValueIn(Math_RgbFromOkLChNode.IdInputC).MapToInputPort(unit.c).SetType(TypeRestriction.LimitToFloat);
            node.ValueIn(Math_RgbFromOkLChNode.IdInputH).MapToInputPort(unit.h).SetType(TypeRestriction.LimitToFloat);

            ColorFunctions.Combine(unitExporter,
                    node.ValueOut(Math_RgbFromOkLChNode.IdOutputR).ExpectedType(ExpectedType.Float),
                    node.ValueOut(Math_RgbFromOkLChNode.IdOutputG).ExpectedType(ExpectedType.Float),
                    node.ValueOut(Math_RgbFromOkLChNode.IdOutputB).ExpectedType(ExpectedType.Float),
                    unit.alpha)
                .MapToPort(unit.color);
            return true;
        }
    }
}
