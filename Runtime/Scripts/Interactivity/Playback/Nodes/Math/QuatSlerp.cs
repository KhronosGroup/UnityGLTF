using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatSlerp : BehaviourEngineNode
    {
        public MathQuatSlerp(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out float4 a);
            TryEvaluateValue(ConstStrings.B, out float4 b);
            TryEvaluateValue(ConstStrings.C, out float c);

            var d = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

            if (d < 0f)
            {
                d *= -1;
                b *= -1;
            }

            float ka, kb;

            if(Mathf.Approximately(d, 1f))
            {
                ka = 1 - c;
                kb = c;
            }
            else
            {
                var w = math.acos(d);
                ka = math.sin(w * (1 - c)) / math.sin(w);
                kb = math.sin(w * c) / math.sin(w);
            }

            var value = new float4(a.x * ka + b.x * kb,
                a.y * ka + b.y * kb,
                a.z * ka + b.z * kb,
                a.w * ka + b.w * kb);

            return new Property<float4>(value);
        }
    }
}