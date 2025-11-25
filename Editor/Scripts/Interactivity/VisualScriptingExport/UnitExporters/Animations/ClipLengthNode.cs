using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ClipLengthNode : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(AnimationClip), nameof(AnimationClip.length), new ClipLengthNode());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;

            var clipInput = unit.target;
            
            int animationId = -1;
            if (unitExporter.IsInputLiteralOrDefaultValue(clipInput, out var clip))
            {
                if (clip is AnimationClip animationClip)
                {
                    var animationsComponents = unitExporter.Context.exporter.RootTransforms.SelectMany(r => r.GetComponentsInChildren<Animation>());
                    
                    var usedClip = animationsComponents.FirstOrDefault(c => AnimationUtility.GetAnimationClips(c.gameObject).Contains(animationClip));
                    if (usedClip != null)
                    {
                        animationId = unitExporter.vsExportContext.exporter.GetAnimationId(animationClip, usedClip.transform);
                    }

                    if (animationId == -1)
                    {
                        var animatorsComponents = unitExporter.Context.exporter.RootTransforms.SelectMany(r => r.GetComponentsInChildren<Animator>());
                        
                        var usedAnimator = animatorsComponents.FirstOrDefault(c => c.runtimeAnimatorController.animationClips.Contains(animationClip));
                        if (usedAnimator != null)
                        {
                            animationId = unitExporter.vsExportContext.exporter.GetAnimationId(animationClip, usedAnimator.transform);
                        }
                    }
                    
                    if (animationId == -1)
                    {
                        UnitExportLogging.AddErrorLog(unit, "Animation not found in export context.");
                        return false;
                    }
                }
                else
                    return false;
            }
            else
            {
                if (clipInput.hasValidConnection && clipInput.connections.First().source.unit is GetMember getMember)
                {
                    if (getMember.target.type == typeof(Animation))
                    {
                        var animationUnit = clipInput.connections.First().source.unit as GetMember;
                        GameObject target = UnitsHelper.GetGameObjectFromValueInput(
                            animationUnit.target, animationUnit.defaultValues, unitExporter.vsExportContext);

                        if (target != null)
                        {
                            animationId = unitExporter.vsExportContext.exporter.GetAnimationId(target.GetComponent<Animation>().clip, target.transform);
                        }
                        
                    }
                }
                
            }

            if (animationId == -1)
                return false;
            
            var length = AnimationHelper.GetAnimationLength(unitExporter, animationId);
            length.MapToPort(unit.value);
            return true;

        }
    }
}