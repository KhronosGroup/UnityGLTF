using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine3x3 : BehaviourEngineNode
    {
        public MathCombine3x3(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            Span<Property<float>> v = stackalloc Property<float>[9];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop is not Property<float> aFloat)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = aFloat;
            }

            var c0 = new float3(v[0].value, v[1].value, v[2].value);
            var c1 = new float3(v[3].value, v[4].value, v[5].value);
            var c2 = new float3(v[6].value, v[7].value, v[8].value);

            return new Property<float3x3>(new float3x3(c0, c1, c2));
        }
    }
}