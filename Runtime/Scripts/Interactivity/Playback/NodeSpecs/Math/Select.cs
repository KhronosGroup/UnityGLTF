using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSelectSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Argument.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2), typeof(float3x3), typeof(float4x4) }), // maybe add more types?
                new NodeValue(ConstStrings.B, "Argument.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2), typeof(float3x3), typeof(float4x4) }),
                new NodeValue(ConstStrings.CONDITION, "Condition.", new Type[]  { typeof(bool) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Select A or B by Condition.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2), typeof(float3x3), typeof(float4x4) }),
            };

            return (null, values);
        }
    }
}