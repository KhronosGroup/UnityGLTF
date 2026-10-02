using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatFromAnglesSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.ORDER, "The rotation order; yxz in the default configuration.", typeof(string)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.X, "Rotation around X in radians.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.Y, "Rotation around Y in radians.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.Z, "Rotation around Z in radians.", new Type[] { typeof(float) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Rotation quaternion.", new Type[] { typeof(float4) }),
            };

            return (null, values);
        }
    }
}
