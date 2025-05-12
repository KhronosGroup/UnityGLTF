using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class PointerSetSpecs : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.POINTER, "JSON Pointer to set.", typeof(string)),
                new NodeConfiguration(ConstStrings.TYPE, "JSON Pointer to set.", typeof(int)),
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
                new NodeValue(ConstStrings.VALUE, "Value to set.", new Type[]  { typeof(int), typeof(float), typeof(float2), typeof(float3), typeof(float4) }),
            };

            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.OUT, "The flow to trigger immediately after execution."),
                new NodeFlow(ConstStrings.ERR, "The flow to trigger in case of an error.")
            };

            return (flows, null);
        }
    }
}