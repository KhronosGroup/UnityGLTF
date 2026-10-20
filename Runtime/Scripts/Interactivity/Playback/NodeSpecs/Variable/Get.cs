using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableGetSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.VARIABLE, "Variable to get.", typeof(string)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Returned value.", new Type[]  { typeof(int), typeof(float), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2), typeof(float3x3), typeof(float4x4) }),
            };

            return (null, values);
        }
    }
}