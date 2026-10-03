using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine3x3 : BehaviourEngineNode
    {
        public MathCombine3x3(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            Span<float> v = stackalloc float[9];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop.type != VariantType.Float)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = prop.Float;
            }

            var c0 = new float3(v[0], v[1], v[2]);
            var c1 = new float3(v[3], v[4], v[5]);
            var c2 = new float3(v[6], v[7], v[8]);

            return Variant.FromFloat3x3(new float3x3(c0, c1, c2));
        }
    }
}