using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine4x4 : BehaviourEngineNode
    {
        public MathCombine4x4(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            Span<float> v = stackalloc float[16];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop.type != VariantType.Float)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = prop.Float;
            }

            var c0 = new float4(v[0], v[1], v[2], v[3]);
            var c1 = new float4(v[4], v[5], v[6], v[7]);
            var c2 = new float4(v[8], v[9], v[10], v[11]);
            var c3 = new float4(v[12], v[13], v[14], v[15]);


            return Variant.FromFloat4x4(new float4x4(c0, c1, c2, c3));
        }
    }
}