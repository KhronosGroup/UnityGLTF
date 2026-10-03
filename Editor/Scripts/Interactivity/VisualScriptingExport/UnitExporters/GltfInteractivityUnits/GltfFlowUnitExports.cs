using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    // Exporters for the glTF flow/math units in GltfFlowUnits.cs. Each unit maps 1:1 to one interactivity node.

    public class GltfSetDelayUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfSetDelay); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfSetDelayUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfSetDelay;
            var node = unitExporter.CreateNode<Flow_SetDelayNode>();
            node.FlowIn(Flow_SetDelayNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowIn(Flow_SetDelayNode.IdFlowInCancel).MapToControlInput(unit.cancel);
            node.ValueIn(Flow_SetDelayNode.IdDuration).MapToInputPort(unit.duration).SetType(TypeRestriction.LimitToFloat);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_SetDelayNode.IdFlowOut, node);
            unitExporter.MapOutFlowConnectionWhenValid(unit.err, Flow_SetDelayNode.IdFlowOutError, node);
            unitExporter.MapOutFlowConnectionWhenValid(unit.done, Flow_SetDelayNode.IdFlowDone, node);
            node.ValueOut(Flow_SetDelayNode.IdOutLastDelay).MapToPort(unit.lastDelay).ExpectedType(ExpectedType.Ref);
            return true;
        }
    }

    public class GltfCancelDelayUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfCancelDelay); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfCancelDelayUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfCancelDelay;
            var node = unitExporter.CreateNode<Flow_CancelDelayNode>();
            node.FlowIn(Flow_CancelDelayNode.IdFlowIn).MapToControlInput(unit.enter);
            node.ValueIn(Flow_CancelDelayNode.IdDelay).MapToInputPort(unit.delay).SetType(TypeRestriction.LimitToRef);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_CancelDelayNode.IdFlowOut, node);
            return true;
        }
    }

    public class GltfThrottleUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfThrottle); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfThrottleUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfThrottle;
            var node = unitExporter.CreateNode<Flow_ThrottleNode>();
            node.FlowIn(Flow_ThrottleNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowIn(Flow_ThrottleNode.IdFlowReset).MapToControlInput(unit.reset);
            node.ValueIn(Flow_ThrottleNode.IdInputDuration).MapToInputPort(unit.duration).SetType(TypeRestriction.LimitToFloat);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_ThrottleNode.IdFlowOut, node);
            unitExporter.MapOutFlowConnectionWhenValid(unit.err, Flow_ThrottleNode.IdFlowOutError, node);
            node.ValueOut(Flow_ThrottleNode.IdOutElapsedTime).MapToPort(unit.lastRemainingTime).ExpectedType(ExpectedType.Float);
            return true;
        }
    }

    public class GltfDoNUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfDoN); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfDoNUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfDoN;
            var node = unitExporter.CreateNode<Flow_DoNNode>();
            node.FlowIn(Flow_DoNNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowIn(Flow_DoNNode.IdFlowReset).MapToControlInput(unit.reset);
            node.ValueIn(Flow_DoNNode.IdN).MapToInputPort(unit.n).SetType(TypeRestriction.LimitToInt);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_DoNNode.IdOut, node);
            node.ValueOut(Flow_DoNNode.IdCurrentExecutionCount).MapToPort(unit.currentCount).ExpectedType(ExpectedType.Int);
            return true;
        }
    }

    public class GltfMultiGateUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfMultiGate); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfMultiGateUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfMultiGate;
            var node = unitExporter.CreateNode<Flow_MultiGateNode>();
            node.Configuration[Flow_MultiGateNode.IdConfigIsRandom].Value = unit.isRandom;
            node.Configuration[Flow_MultiGateNode.IdConfigIsLoop].Value = unit.isLoop;
            node.FlowIn(Flow_MultiGateNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowIn(Flow_MultiGateNode.IdFlowInReset).MapToControlInput(unit.reset);
            // output sockets are dynamic (not in the schema), so they have to be created here
            for (int i = 0; i < unit.outputs.Count; i++)
                node.FlowOut(i.ToString()).MapToControlOutput(unit.outputs[i]);
            node.ValueOut(Flow_MultiGateNode.IdLastIndex).MapToPort(unit.lastIndex).ExpectedType(ExpectedType.Int);
            return true;
        }
    }

    public class GltfIntBitwiseUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GltfIntBitwise); }

        [InitializeOnLoadMethod]
        private static void Register() => UnitExporterRegistry.RegisterExporter(new GltfIntBitwiseUnitExport());

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GltfIntBitwise;
            GltfInteractivityExportNode node;
            switch (unit.operation)
            {
                case GltfIntBitwise.Operation.And: node = unitExporter.CreateNode<Math_AndNode>(); break;
                case GltfIntBitwise.Operation.Or: node = unitExporter.CreateNode<Math_OrNode>(); break;
                case GltfIntBitwise.Operation.Xor: node = unitExporter.CreateNode<Math_XorNode>(); break;
                case GltfIntBitwise.Operation.Not: node = unitExporter.CreateNode<Math_NotNode>(); break;
                case GltfIntBitwise.Operation.ShiftRight: node = unitExporter.CreateNode<Math_AsrNode>(); break;
                case GltfIntBitwise.Operation.ShiftLeft: node = unitExporter.CreateNode<Math_LslNode>(); break;
                case GltfIntBitwise.Operation.CountLeadingZeros: node = unitExporter.CreateNode<Math_ClzNode>(); break;
                case GltfIntBitwise.Operation.CountTrailingZeros: node = unitExporter.CreateNode<Math_CtzNode>(); break;
                case GltfIntBitwise.Operation.PopCount: node = unitExporter.CreateNode<Math_PopcntNode>(); break;
                default:
                    UnitExportLogging.AddErrorLog(unit, "Unsupported operation: " + unit.operation);
                    return false;
            }

            node.ValueIn("a").MapToInputPort(unit.a).SetType(TypeRestriction.LimitToInt);
            if (unit.b != null)
                node.ValueIn("b").MapToInputPort(unit.b).SetType(TypeRestriction.LimitToInt);
            node.FirstValueOut().MapToPort(unit.value).ExpectedType(ExpectedType.Int);
            return true;
        }
    }
}
