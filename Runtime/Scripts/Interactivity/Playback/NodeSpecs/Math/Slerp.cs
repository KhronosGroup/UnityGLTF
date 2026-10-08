using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSlerpSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "First vector.", new Type[] { typeof(float2), typeof(float3) }),
                new NodeValue(ConstStrings.B, "Second vector.", new Type[] { typeof(float2), typeof(float3) }),
                new NodeValue(ConstStrings.C, "Unclamped interpolation coefficient.", new Type[] { typeof(float) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Interpolated value.", new Type[] { typeof(float2), typeof(float3) }),
            };

            return (null, values);
        }
    }
}
