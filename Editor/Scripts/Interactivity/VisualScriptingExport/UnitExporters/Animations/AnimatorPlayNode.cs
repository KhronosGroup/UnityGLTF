using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Adapts the "Animator.Play" InvokeMember Unity node to a world/startAnimation glTF node.
    /// </summary>
    internal class AnimatorPlayNode : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }

        private readonly string _stateNameKey = "%stateName";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Animator), nameof(Animator.Play),
                new AnimatorPlayNode());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            InvokeMember unit = unitExporter.unit as InvokeMember;
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Animation_StartNode());

            GameObject target = UnitsHelper.GetGameObjectFromValueInput(
                unit.target, unit.defaultValues, unitExporter.exportContext);

            if (target == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target GameObject");
                return false;
            }

            // Get the state name from the node
            if (!UnitsHelper.GetDefaultValue<string>(unit, _stateNameKey, out string stateName))
            {
                UnitExportLogging.AddErrorLog(unit, "Invalid state name.");
                return false;
            }
            
            var animationState = AnimatorHelper.GetAnimationState(target, stateName);
            int animationId = AnimatorHelper.GetAnimationId(animationState, unitExporter.exportContext);

            if (animationId == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Animation not found in export context.");
                return false;
            }

            node.ValueSocketConnectionData[Animation_StartNode.IdValueAnimation].Value = animationId;
            node.ValueSocketConnectionData[Animation_StartNode.IdValueSpeed].Value = animationState.speed;

            // TODO: Get from clip start from state cycleOffset
            node.ValueSocketConnectionData[Animation_StartNode.IdValueStartTime].Value = 0.0f;

            var otherAnimationStates = AnimatorHelper.GetAllAnimationStates(target);
            var stopSequence = unitExporter.CreateNode(new Flow_SequenceNode());
            stopSequence.FlowIn(Flow_SequenceNode.IdFlowIn).MapToControlInput(unit.enter);
       
            int index = 0;
            foreach (var state in otherAnimationStates)
            {
                if (state == animationState) continue;
                var stopNode = unitExporter.CreateNode(new Animation_StopNode());
                stopNode.ValueIn(Animation_StopNode.IdValueAnimation)
                    .SetValue(AnimatorHelper.GetAnimationId(state, unitExporter.exportContext));

                var sequFlowId = "sequ" + index.ToString("D3");
                index++;
                stopSequence.FlowOut(sequFlowId).ConnectToFlowDestination(stopNode.FlowIn(Animation_StopNode.IdFlowIn));
            }

            var lastSequenceFlow = stopSequence.FlowOut("zzz");
            lastSequenceFlow.ConnectToFlowDestination(node.FlowIn(Animation_StartNode.IdFlowIn));
            
            AnimationClip clip = animationState.motion as AnimationClip;
            node.ValueSocketConnectionData[Animation_StartNode.IdValueEndtime].Value =
                (clip != null && !clip.isLooping) ? clip.length : float.PositiveInfinity;

            // There should only be one output flow from the Animator.Play node
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Animation_StartNode.IdFlowOut, node);
            return true;
        }
        
    }
}