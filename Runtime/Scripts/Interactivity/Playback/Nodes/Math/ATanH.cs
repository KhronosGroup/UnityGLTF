using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathATanH : BehaviourEngineNode
    {
        public MathATanH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(ATanH(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(ATanH(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(ATanH(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(ATanH(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private float ATanH(float x)
        {
            // 0.5 * ln((1+x)/(1-x))
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float2 ATanH(float2 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float3 ATanH(float3 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float4 ATanH(float4 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }
    }
}