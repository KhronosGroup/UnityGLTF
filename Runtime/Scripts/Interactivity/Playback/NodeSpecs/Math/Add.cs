using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathAddSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Argument.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4) }),
                new NodeValue(ConstStrings.B, "Argument.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Add A with B.", new Type[]  { typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4) }),
            };

            return (null, values);
        }
    }
}