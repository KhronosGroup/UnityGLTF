using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class DebugAssertSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Condition", new Type[]  { typeof(bool) }),
                new NodeValue(ConstStrings.B, "Parameter 1", new Type[]  { typeof(bool), typeof(int), typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4) }),
                new NodeValue(ConstStrings.C, "Parameter 2", new Type[]  { typeof(bool), typeof(int), typeof(float), typeof(int), typeof(float2), typeof(float3), typeof(float4) })
            };

            return (null, values);
        }
    }
}