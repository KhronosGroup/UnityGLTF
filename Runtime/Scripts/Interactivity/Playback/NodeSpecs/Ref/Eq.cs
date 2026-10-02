using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class RefEqSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "First reference.", new Type[] { typeof(Ref) }),
                new NodeValue(ConstStrings.B, "Second reference.", new Type[] { typeof(Ref) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "True if both references are null or refer to the same object.", new Type[] { typeof(bool) }),
            };

            return (null, values);
        }
    }
}
