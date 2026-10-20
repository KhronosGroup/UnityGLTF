using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathACosH : BehaviourEngineNode
    {
        public MathACosH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(ACosH(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(ACosH(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(ACosH(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(ACosH(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private float ACosH(float x)
        {
            // ln(x + sqrt(x^2 - 1))
            return math.log(x + math.sqrt(x * x - 1));
        }

        private float2 ACosH(float2 x)
        {
            return math.log(x + math.sqrt(x * x - 1));
        }

        private float3 ACosH(float3 x)
        {
            return math.log(x + math.sqrt(x * x - 1));
        }

        private float4 ACosH(float4 x)
        {
            return math.log(x + math.sqrt(x * x - 1));
        }
    }
}