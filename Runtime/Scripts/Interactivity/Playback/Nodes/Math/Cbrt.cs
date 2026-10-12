using System;
using Unity.Mathematics;
using UnityEngine;


namespace UnityGLTF.Interactivity.Playback
{
    public class MathCbrt : BehaviourEngineNode
    {
        public MathCbrt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            return a.type switch
            {
                VariantType.Float => Variant.FromFloat(MathF.Cbrt(a.Float)),
                VariantType.Float2 => Variant.FromFloat2(Cbrt(a.Float2)),
                VariantType.Float3 => Variant.FromFloat3(Cbrt(a.Float3)),
                VariantType.Float4 => Variant.FromFloat4(Cbrt(a.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2 Cbrt(float2 v)
        {
            return new float2(MathF.Cbrt(v.x), MathF.Cbrt(v.y));
        }

        private static float3 Cbrt(float3 v)
        {
            return new float3(MathF.Cbrt(v.x), MathF.Cbrt(v.y), MathF.Cbrt(v.z));
        }

        private static float4 Cbrt(float4 v)
        {
            return new float4(MathF.Cbrt(v.x), MathF.Cbrt(v.y), MathF.Cbrt(v.z), MathF.Cbrt(v.w));
        }
    }
}