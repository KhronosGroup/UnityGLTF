using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    internal class AnimationPlayNode : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Animation), nameof(Animation.Play),
                new AnimationPlayNode());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            InvokeMember unit = unitExporter.unit as InvokeMember;

            GameObject target = GltfInteractivityNodeHelper.GetGameObjectFromValueInput(
                unit.target, unit.defaultValues, unitExporter.exportContext);

            if (target == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target GameObject");
                return false;
            }

            var animation = target.GetComponent<Animation>();
            if (!animation)
            {
                UnitExportLogging.AddErrorLog(unit, "Target GameObject does not have an Animation component.");
                return false;
            }

            var clip = animation.clip;
            if (unit.inputParameters.Count > 0)
            {
                if (unit.inputParameters[0].key == "%animation")
                {
                    if (!unitExporter.IsInputLiteralOrDefaultValue(unit.inputParameters[0], out var animationName))
                    {
                        UnitExportLogging.AddErrorLog(unit, "Animation name is not a literal or default value, which is not supported.");
                        return false;
                    }
                    
                    if (animationName is string animationNameString)
                    {
                        clip = animation.GetClip(animationNameString);
                        if (clip == null)
                        {
                            UnitExportLogging.AddErrorLog(unit, "Animation not found in Animation component.");
                            return false;
                        }
                    }
                }
            }
            
            int animationId = unitExporter.exportContext.exporter.GetAnimationId(clip, target.transform);

            if (animationId == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Animation not found in export context.");
                return false;
            }

            var node = unitExporter.CreateNode(new Animation_StartNode());
            node.ValueSocketConnectionData[Animation_StartNode.IdValueAnimation].Value = animationId;
            node.ValueSocketConnectionData[Animation_StartNode.IdValueSpeed].Value = 1f;
            node.ValueSocketConnectionData[Animation_StartNode.IdValueStartTime].Value = 0.0f;

            node.ValueSocketConnectionData[Animation_StartNode.IdValueEndtime].Value =
                (clip != null && !clip.isLooping) ? clip.length : float.PositiveInfinity;
            
            unitExporter.MapInputPortToSocketName(unit.enter, Animation_StartNode.IdFlowIn, node);
            // There should only be one output flow from the Animator.Play node
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Animation_StartNode.IdFlowOut, node);
            return true;
        }
        
    }
}