using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WorldToLocalPositionUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(WorldToLocalPosition); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WorldToLocalPositionUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WorldToLocalPosition;

            TransformHelpersVS.GetLocalPointFromWorldPoint(unitExporter, unit.target, out var worldPoint, out var localPoint);
            worldPoint.MapToInputPort(unit.input);
            localPoint.MapToPort(unit.output);
            return true;
        }
    }

    public class LocalToWorldPositionUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(LocalToWorldPosition); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new LocalToWorldPositionUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as LocalToWorldPosition;

            TransformHelpersVS.GetWorldPointFromLocalPoint(unitExporter, unit.target, out var localPoint, out var worldPoint);
            localPoint.MapToInputPort(unit.input);
            worldPoint.MapToPort(unit.output);
            return true;
        }
    }

    public class WorldToLocalRotationUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(WorldToLocalRotation); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WorldToLocalRotationUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WorldToLocalRotation;

            // localRotation = inverse(target world rotation) * worldRotation
            TransformHelpersVS.GetWorldRotation(unitExporter, unit.target, out var targetRotation);

            var inverseRotation = unitExporter.CreateNode<Math_QuatConjugateNode>();
            inverseRotation.ValueIn(Math_QuatConjugateNode.IdValueA).ConnectToSource(targetRotation);

            var mul = unitExporter.CreateNode<Math_QuatMulNode>();
            mul.ValueIn(Math_QuatMulNode.IdValueA).ConnectToSource(inverseRotation.FirstValueOut());
            mul.ValueIn(Math_QuatMulNode.IdValueB).MapToInputPort(unit.input);
            mul.FirstValueOut().MapToPort(unit.output);
            return true;
        }
    }

    public class LocalToWorldRotationUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(LocalToWorldRotation); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new LocalToWorldRotationUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as LocalToWorldRotation;

            // worldRotation = target world rotation * localRotation
            TransformHelpersVS.GetWorldRotation(unitExporter, unit.target, out var targetRotation);

            var mul = unitExporter.CreateNode<Math_QuatMulNode>();
            mul.ValueIn(Math_QuatMulNode.IdValueA).ConnectToSource(targetRotation);
            mul.ValueIn(Math_QuatMulNode.IdValueB).MapToInputPort(unit.input);
            mul.FirstValueOut().MapToPort(unit.output);
            return true;
        }
    }
}
