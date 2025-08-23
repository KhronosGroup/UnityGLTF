using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRandomSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            return (flows, null);
        }
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Value", new Type[]  { typeof(float) }),
            };

            return (null, values);
        }
    }
}