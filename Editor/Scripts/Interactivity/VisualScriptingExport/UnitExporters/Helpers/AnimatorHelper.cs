using UnityEditor.Animations;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class AnimatorHelper
    {
        public static AnimatorState GetAnimationState(GameObject target, string stateName)
        {
            // The gameobject should have an attached Animator component
            Animator animator = target.GetComponent<Animator>();

            if (animator == null)
            {
                return null;
            }

            AnimatorController animatorController = animator.runtimeAnimatorController as AnimatorController;
            foreach (AnimatorControllerLayer animatorLayer in animatorController.layers)
            {
                ChildAnimatorState[] animatorStates = animatorLayer.stateMachine.states;
                foreach (ChildAnimatorState state in animatorStates)
                {
                    Debug.Log("Found a state called " + state.state.name + " with a speed of " + state.state.speed +
                              " with a length of " + state.state.motion.averageDuration);
                    if (state.state.name == stateName)
                    {
                        return state.state;
                    }
                }
            }

            return null;
        }

        public static  int GetAnimationId(AnimatorState state, VisualScriptingExportContext exportContext)
        {
            string animationName = state.motion.name;
            return UnitsHelper.GetNamedPropertyGltfIndex(animationName,
                exportContext.ActiveGltfRoot.Animations);
        }
    }
}