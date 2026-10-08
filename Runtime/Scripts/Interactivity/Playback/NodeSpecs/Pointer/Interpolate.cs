using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerInterpolateSpecs : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.POINTER, "JSON Pointer to interpolate.", typeof(string)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.DURATION, "Interpolation duration.", new Type[]  { typeof(float) }),
                new NodeValue(ConstStrings.VALUE, "Target value to interpolate to.", new Type[]  { typeof(int), typeof(float), typeof(float2), typeof(float3), typeof(float4) }),
                new NodeValue(ConstStrings.P1, "Control point 1.", new Type[]  { typeof(float2) }),
                new NodeValue(ConstStrings.P2, "Control point 2.", new Type[]  { typeof(float2) }),
            };

            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.DONE, "The flow to trigger after interpolation finishes."),
                new NodeFlow(ConstStrings.OUT, "The flow to trigger immediately after execution."),
                new NodeFlow(ConstStrings.ERR, "The flow to trigger in case of an error.")
            };

            return (flows, null);
        }
    }
}