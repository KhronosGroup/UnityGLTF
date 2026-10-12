using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRgbToOkLChSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.R, "Red.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.G, "Green.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.B, "Blue.", new Type[] { typeof(float) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.L, "Lightness.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.C, "Chroma.", new Type[] { typeof(float) }),
                new NodeValue(ConstStrings.H, "Hue in radians.", new Type[] { typeof(float) }),
            };

            return (null, values);
        }
    }
}
