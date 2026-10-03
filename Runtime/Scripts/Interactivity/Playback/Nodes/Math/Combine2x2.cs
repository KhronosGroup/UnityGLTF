using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine2x2 : BehaviourEngineNode
    {
        public MathCombine2x2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            Span<float> v = stackalloc float[4];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop.type != VariantType.Float)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = prop.Float;
            }

            var c0 = new float2(v[0], v[1]);
            var c1 = new float2(v[2], v[3]);

            return Variant.FromFloat2x2(new float2x2(c0, c1));
        }
    }
}