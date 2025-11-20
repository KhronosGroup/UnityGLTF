using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathAbsSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Argument.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2),  typeof(float3x3),  typeof(float4x4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "If a > 0 then -a, else a.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2),  typeof(float3x3),  typeof(float4x4) }),
            };

            return (null, values);
        }
    }
}