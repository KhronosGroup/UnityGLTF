using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class NullRefHelper
    {
        /// <summary>
        /// True when the input can never be null (e.g. a Vector3), so null checks can be skipped.
        /// </summary>
        public static bool IsValueTypeInput(ValueInput input)
        {
            if (input.hasValidConnection)
                return input.connection.source.type.IsValueType;
            if (input.hasDefaultValue && input.unit.defaultValues.TryGetValue(input.key, out var value) && value != null)
                return value.GetType().IsValueType;
            return false;
        }

        /// <summary>
        /// Compares a reference with the null reference (an empty pointer).
        /// </summary>
        public static ValueOutRef IsNull(UnitExporter unitExporter, ValueInput input)
        {
            var eqNode = unitExporter.CreateNode<Ref_EqNode>();
            eqNode.ValueIn(Ref_EqNode.IdValueA).MapToInputPort(input).SetType(TypeRestriction.LimitToRef);
            eqNode.ValueIn(Ref_EqNode.IdValueB).SetValue(new StaticRefPointer()).SetType(TypeRestriction.LimitToRef);
            return eqNode.FirstValueOut().ExpectedType(ExpectedType.Bool);
        }
    }

    public class NullCheckUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(NullCheck); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new NullCheckUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as NullCheck;

            if (NullRefHelper.IsValueTypeInput(unit.input))
            {
                // Value types are never null
                unitExporter.ByPassFlow(unit.enter, unit.ifNotNull);
                return true;
            }

            var branch = unitExporter.CreateNode<Flow_BranchNode>();
            branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(NullRefHelper.IsNull(unitExporter, unit.input));
            branch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter);
            branch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.ifNull);
            branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.ifNotNull);
            return true;
        }
    }

    public class NullCoalesceUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(NullCoalesce); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new NullCoalesceUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as NullCoalesce;

            if (NullRefHelper.IsValueTypeInput(unit.input))
            {
                // Value types are never null
                unitExporter.ByPassValue(unit.input, unit.result);
                return true;
            }

            var select = unitExporter.CreateNode<Math_SelectNode>();
            select.ValueIn(Math_SelectNode.IdCondition).ConnectToSource(NullRefHelper.IsNull(unitExporter, unit.input));
            select.ValueIn(Math_SelectNode.IdValueA).MapToInputPort(unit.fallback).SetType(TypeRestriction.LimitToRef);
            select.ValueIn(Math_SelectNode.IdValueB).MapToInputPort(unit.input).SetType(TypeRestriction.LimitToRef);
            select.FirstValueOut().ExpectedType(ExpectedType.Ref).MapToPort(unit.result);
            return true;
        }
    }
}
