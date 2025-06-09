using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatConjugateSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Input quaternion", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Conjugated quaternion.", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }
    }

    public class MathQuatMulSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "First quaternion", new Type[]  { typeof(float4) }),
                new NodeValue(ConstStrings.B, "Second quaternion", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Quaternion product.", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }
    }

    public class MathQuatAngleBetweenSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "First quaternion", new Type[]  { typeof(float4) }),
                new NodeValue(ConstStrings.B, "Second quaternion", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Angle in radians.", new Type[]  { typeof(float) }),
            };

            return (null, values);
        }
    }

    public class MathQuatFromAxisAngleSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.AXIS, "Rotation axis.", new Type[]  { typeof(float3) }),
                new NodeValue(ConstStrings.ANGLE, "Angle in Radians.", new Type[]  { typeof(float) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Rotation quaternion.", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }
    }

    public class MathQuatToAxisAngleSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Rotation quaternion.", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.AXIS, "Rotation axis.", new Type[]  { typeof(float3) }),
                new NodeValue(ConstStrings.ANGLE, "Angle in Radians.", new Type[]  { typeof(float) }),
            };

            return (null, values);
        }
    }

    public class MathQuatFromDirectionsSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "First direction.", new Type[]  { typeof(float3) }),
                new NodeValue(ConstStrings.B, "Second direction.", new Type[]  { typeof(float3) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Rotation quaternion.", new Type[]  { typeof(float4) }),
            };

            return (null, values);
        }
    }
}