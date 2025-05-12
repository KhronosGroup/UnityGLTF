using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine4x4 : BehaviourEngineNode
    {
        public MathCombine4x4(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            Span<Property<float>> v = stackalloc Property<float>[16];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop is not Property<float> aFloat)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = (Property<float>)prop;
            }

            var c0 = new float4(v[0].value, v[1].value, v[2].value, v[3].value);
            var c1 = new float4(v[4].value, v[5].value, v[6].value, v[7].value);
            var c2 = new float4(v[8].value, v[9].value, v[10].value, v[11].value);
            var c3 = new float4(v[12].value, v[13].value, v[14].value, v[15].value);


            return new Property<float4x4>(new float4x4(c0, c1, c2, c3));
        }
    }
}