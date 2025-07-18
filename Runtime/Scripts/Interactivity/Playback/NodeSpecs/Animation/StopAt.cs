using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class AnimationStopAtSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.ANIMATION, "Animation index.", new Type[]  { typeof(int) }),
                new NodeValue(ConstStrings.STOP_TIME, "Stop time in seconds.", new Type[]  { typeof(float) }),
            };

            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.DONE, "The flow to be activated after the animation stops."),
                new NodeFlow(ConstStrings.OUT, "The flow to trigger immediately after execution."),
                new NodeFlow(ConstStrings.ERR, "The flow to trigger in case of an error.")
            };

            return (flows, null);
        }
    }
}