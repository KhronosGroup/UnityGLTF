using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCombine2x2 : BehaviourEngineNode
    {
        public MathCombine2x2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            Span<Property<float>> v = stackalloc Property<float>[4];

            for (int i = 0; i < v.Length; i++)
            {
                TryEvaluateValue(ConstStrings.Letters[i], out var prop);

                if (prop is not Property<float> aFloat)
                    throw new InvalidOperationException($"Input {ConstStrings.Letters[i]} is not a float!");

                v[i] = (Property<float>)prop;
            }

            var c0 = new float2(v[0].value, v[1].value);
            var c1 = new float2(v[2].value, v[3].value);

            return new Property<float2x2>(new float2x2(c0, c1));
        }
    }
}