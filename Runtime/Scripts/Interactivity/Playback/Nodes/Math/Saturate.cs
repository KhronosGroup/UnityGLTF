using System;
using Unity.Mathematics;
using UnityEngine;
using static UnityGLTF.Interactivity.Playback.MathMax;
using static UnityGLTF.Interactivity.Playback.MathMin;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSaturate : BehaviourEngineNode
    {
        public MathSaturate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        // Spec: min(max(a, 0), 1) using math/min and math/max, so NaN propagates (math.saturate returns 0).
        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(Min(Max(a.Float, 0f), 1f)),
                VariantType.Float2 => Variant.FromFloat2(Min(Max(a.Float2, float2.zero), new float2(1f))),
                VariantType.Float3 => Variant.FromFloat3(Min(Max(a.Float3, float3.zero), new float3(1f))),
                VariantType.Float4 => Variant.FromFloat4(Min(Max(a.Float4, float4.zero), new float4(1f))),
                VariantType.Float2x2 => Variant.FromFloat2x2(Min(Max(a.Float2x2, float2x2.zero), new float2x2(1f))),
                VariantType.Float3x3 => Variant.FromFloat3x3(Min(Max(a.Float3x3, float3x3.zero), new float3x3(1f))),
                VariantType.Float4x4 => Variant.FromFloat4x4(Min(Max(a.Float4x4, float4x4.zero), new float4x4(1f))),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}
