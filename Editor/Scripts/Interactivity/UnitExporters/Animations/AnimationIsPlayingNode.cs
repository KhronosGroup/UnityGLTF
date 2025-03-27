using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    internal class AnimationIsPlayingNode : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Animation), nameof(Animation.IsPlaying), new AnimationIsPlayingNode());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Animation), nameof(Animation.isPlaying), new AnimationIsPlayingNode());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MemberUnit;

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
            if (unit is InvokeMember invokeMember && invokeMember.inputParameters.Count > 0)
            {
                unitExporter.ByPassFlow(invokeMember.enter, invokeMember.exit);
                if (invokeMember.inputParameters[0].key == "%animation")
                {
                    if (!unitExporter.IsInputLiteralOrDefaultValue(invokeMember.inputParameters[0], out var animationName))
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

            var node = unitExporter.CreateNode(new Pointer_GetNode());
            node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerAnimationIndex, "/animations/{" + GltfInteractivityNodeHelper.IdPointerAnimationIndex + "}/extensions/KHR_interactivity/isPlaying", GltfTypes.Bool);
            node.ValueIn(GltfInteractivityNodeHelper.IdPointerAnimationIndex).SetValue(animationId)
                .SetType(TypeRestriction.LimitToInt);
            
            node.FirstValueOut().MapToPort(unit.valueOutputs[0]).ExpectedType(ExpectedType.Bool);
            return true;
        }
        
    }
}