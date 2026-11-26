using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class InterpolateTransformUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InterpolateTransform); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new InterpolateTransformUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InterpolateTransform;

            if (!unit.position && !unit.rotation && !unit.scale)
            {
                UnitExportLogging.AddWarningLog(unit, "Interpolate Transform has position, rotation and scale disabled.");
                unitExporter.ByPassFlow(unit.assign, unit.assigned);
                return true;
            }

            // Everything stays in glTF space, so no space conversion is needed
            ValueOutRef translation, rotation, scale;
            if (TransformHelpersVS.HaveSameExportedParent(unitExporter, unit.target, unit.targetValue))
            {
                // Same parent: the local transform of the target value is the local target, no world<>local conversion needed
                translation = unit.position ? GetLocalProperty(unitExporter, unit.targetValue, "/translation", GltfTypes.Float3) : null;
                rotation = unit.rotation ? GetLocalProperty(unitExporter, unit.targetValue, "/rotation", GltfTypes.Float4) : null;
                scale = unit.scale ? GetLocalProperty(unitExporter, unit.targetValue, "/scale", GltfTypes.Float3) : null;
            }
            else
                GetLocalTargetFromWorld(unitExporter, unit, out translation, out rotation, out scale);

            var interpolations = new List<GltfInteractivityExportNode>();
            if (unit.position)
                interpolations.Add(CreateInterpolation(unitExporter, unit, "/translation", GltfTypes.Float3, translation));
            if (unit.rotation)
                interpolations.Add(CreateInterpolation(unitExporter, unit, "/rotation", GltfTypes.Float4, rotation));
            if (unit.scale)
                interpolations.Add(CreateInterpolation(unitExporter, unit, "/scale", GltfTypes.Float3, scale));

            // Start all interpolations in a row, they share the same duration and easing
            interpolations[0].FlowIn(Pointer_InterpolateNode.IdFlowIn).MapToControlInput(unit.assign);
            for (int i = 1; i < interpolations.Count; i++)
                interpolations[i - 1].FlowOut(Pointer_InterpolateNode.IdFlowOut)
                    .ConnectToFlowDestination(interpolations[i].FlowIn(Pointer_InterpolateNode.IdFlowIn));
            interpolations[interpolations.Count - 1].FlowOut(Pointer_InterpolateNode.IdFlowOut).MapToControlOutput(unit.assigned);

            if (interpolations.Count == 1)
            {
                interpolations[0].FlowOut(Pointer_InterpolateNode.IdFlowOutDone).MapToControlOutput(unit.done);
                return true;
            }

            // Done is triggered when all interpolations are done
            var waitAll = unitExporter.CreateNode<Flow_WaitAllNode>();
            waitAll.Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value = interpolations.Count;
            for (int i = 0; i < interpolations.Count; i++)
                interpolations[i].FlowOut(Pointer_InterpolateNode.IdFlowOutDone)
                    .ConnectToFlowDestination(waitAll.FlowIn(i.ToString()));
            waitAll.FlowOut(Flow_WaitAllNode.IdFlowOutCompleted).MapToControlOutput(unit.done);

            return true;
        }

        private static ValueOutRef GetLocalProperty(UnitExporter unitExporter, ValueInput target, string property, string valueType)
        {
            var get = unitExporter.CreateNode<Pointer_GetNode>();
            PointersHelperVS.SetupPointerTemplateAndTargetInput(get, PointersHelper.IdPointerNodeRef, target,
                PointersHelper.IdPointerTemplNodeByRef + property, valueType);
            return get.FirstValueOut();
        }

        // localTarget = inverse(parentWorld) * targetValueWorld, with inverse(parentWorld) = localMatrix * inverse(globalMatrix)
        private static void GetLocalTargetFromWorld(UnitExporter unitExporter, InterpolateTransform unit,
            out ValueOutRef translation, out ValueOutRef rotation, out ValueOutRef scale)
        {
            TransformHelpersVS.GetWorldMatrix(unitExporter, unit.target, out var globalMatrix);

            var localMatrix = unitExporter.CreateNode<Pointer_GetNode>();
            PointersHelperVS.SetupPointerTemplateAndTargetInput(localMatrix, PointersHelper.IdPointerNodeRef, unit.target,
                PointersHelper.IdPointerTemplNodeByRef + "/matrix", GltfTypes.Float4x4);

            var inverseGlobal = unitExporter.CreateNode<Math_InverseNode>();
            inverseGlobal.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(globalMatrix);

            var inverseParent = unitExporter.CreateNode<Math_MatMulNode>();
            inverseParent.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(localMatrix.FirstValueOut());
            inverseParent.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(inverseGlobal.FirstValueOut());

            TransformHelpersVS.GetWorldMatrix(unitExporter, unit.targetValue, out var targetValueMatrix);

            var localTarget = unitExporter.CreateNode<Math_MatMulNode>();
            localTarget.ValueIn(Math_MatMulNode.IdValueA).ConnectToSource(inverseParent.FirstValueOut());
            localTarget.ValueIn(Math_MatMulNode.IdValueB).ConnectToSource(targetValueMatrix);

            var decompose = unitExporter.CreateNode<Math_MatDecomposeNode>();
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(localTarget.FirstValueOut());

            translation = decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation);
            rotation = decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation);
            scale = decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale);
        }

        private static GltfInteractivityExportNode CreateInterpolation(UnitExporter unitExporter, InterpolateTransform unit,
            string property, string valueType, ValueOutRef value)
        {
            var node = unitExporter.CreateNode<Pointer_InterpolateNode>();
            PointersHelperVS.SetupPointerTemplateAndTargetInput(node, PointersHelper.IdPointerNodeRef, unit.target,
                PointersHelper.IdPointerTemplNodeByRef + property, valueType);

            node.ValueIn(Pointer_InterpolateNode.IdValue).ConnectToSource(value);
            node.ValueIn(Pointer_InterpolateNode.IdDuration).MapToInputPort(unit.duration);
            node.ValueIn(Pointer_InterpolateNode.IdPoint1).MapToInputPort(unit.pointA);
            node.ValueIn(Pointer_InterpolateNode.IdPoint2).MapToInputPort(unit.pointB);
            return node;
        }
    }
}
