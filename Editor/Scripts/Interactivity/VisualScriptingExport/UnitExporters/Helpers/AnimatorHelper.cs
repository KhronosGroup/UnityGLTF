using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class AnimatorHelper 
    {
        public static AnimatorState GetAnimationState(GameObject target, string stateName)
        {
            return GetAllAnimationStates(target).FirstOrDefault(x => x.name == stateName);
        }

        public static IEnumerable<AnimatorState> GetAllAnimationStates(GameObject target)
        {   
            Animator animator = target.GetComponent<Animator>();

            if (!animator) yield break;

            AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
            if (!animatorController) yield break;
            
            foreach (AnimatorControllerLayer animatorLayer in animatorController.layers)
            {
                ChildAnimatorState[] animatorStates = animatorLayer.stateMachine.states;
                foreach (ChildAnimatorState state in animatorStates)
                {
                    // Debug.Log("Found a state called " + state.state.name + " with a speed of " + state.state.speed +          " with a length of " + state.state.motion.averageDuration);
                    yield return state.state;
                }
            }
        }
    }
}