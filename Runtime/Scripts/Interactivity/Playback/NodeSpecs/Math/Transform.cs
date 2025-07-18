using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTransformSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Vector", new Type[]  { typeof(float2), typeof(float3), typeof(float4)}),
                new NodeValue(ConstStrings.B, "Matrix", new Type[]  { typeof(float2x2), typeof(float3x3), typeof(float4x4)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Value", new Type[]  { typeof(float2), typeof(float3), typeof(float4) }),
            };

            return (null, values);
        }
    }
}