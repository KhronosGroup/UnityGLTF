using System;
using Unity.Mathematics;
using UnityEngine;
using static UnityGLTF.Interactivity.Playback.MathMax;
using static UnityGLTF.Interactivity.Playback.MathMin;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathClamp : BehaviourEngineNode
    {
        public MathClamp(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        // Spec: min(max(a, min(b, c)), max(b, c)), which tolerates b > c and propagates NaN (math.clamp does neither).
        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);
            TryEvaluateValue(ConstStrings.C, out Variant c);

            return a.type switch
            {
                VariantType.Int when b.type == VariantType.Int && c.type == VariantType.Int => Variant.FromInt(math.min(math.max(a.Int, math.min(b.Int, c.Int)), math.max(b.Int, c.Int))),
                VariantType.Float when b.type == VariantType.Float && c.type == VariantType.Float => Variant.FromFloat(Min(Max(a.Float, Min(b.Float, c.Float)), Max(b.Float, c.Float))),
                VariantType.Float2 when b.type == VariantType.Float2 && c.type == VariantType.Float2 => Variant.FromFloat2(Min(Max(a.Float2, Min(b.Float2, c.Float2)), Max(b.Float2, c.Float2))),
                VariantType.Float3 when b.type == VariantType.Float3 && c.type == VariantType.Float3 => Variant.FromFloat3(Min(Max(a.Float3, Min(b.Float3, c.Float3)), Max(b.Float3, c.Float3))),
                VariantType.Float4 when b.type == VariantType.Float4 && c.type == VariantType.Float4 => Variant.FromFloat4(Min(Max(a.Float4, Min(b.Float4, c.Float4)), Max(b.Float4, c.Float4))),
                VariantType.Float2x2 when b.type == VariantType.Float2x2 && c.type == VariantType.Float2x2 => Variant.FromFloat2x2(Min(Max(a.Float2x2, Min(b.Float2x2, c.Float2x2)), Max(b.Float2x2, c.Float2x2))),
                VariantType.Float3x3 when b.type == VariantType.Float3x3 && c.type == VariantType.Float3x3 => Variant.FromFloat3x3(Min(Max(a.Float3x3, Min(b.Float3x3, c.Float3x3)), Max(b.Float3x3, c.Float3x3))),
                VariantType.Float4x4 when b.type == VariantType.Float4x4 && c.type == VariantType.Float4x4 => Variant.FromFloat4x4(Min(Max(a.Float4x4, Min(b.Float4x4, c.Float4x4)), Max(b.Float4x4, c.Float4x4))),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}
