using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatFromDirections : BehaviourEngineNode
    {
        private static readonly float4 IDENTITY = new float4(0f, 0f, 0f, 1f);

        public MathQuatFromDirections(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Float3 when b.type == VariantType.Float3 => Variant.FromFloat4(FromDirections(a.Float3, b.Float3)),
                _ => throw new InvalidOperationException($"Input A is a {a.GetTypeSignature()} and not a float4!"),
            };
        }

        private static float4 FromDirections(float3 a, float3 b)
        {
            var c = math.dot(a, b);

            if (Mathf.Approximately(c, 1f))
                return IDENTITY;

            if (Mathf.Approximately(c, -1f))
                return GeneratePerpendicularUnitVector(a);

            var halfc = 0.5f * c;
            var r = math.normalize(math.cross(a, b));
            r *= math.sqrt(0.5f - halfc);

            return new float4(r.x, r.y, r.z, math.sqrt(0.5f + halfc));
        }

        private static float4 GeneratePerpendicularUnitVector(float3 a)
        {
            var x = CopySign(a.z, a.x);
            var y = CopySign(a.z, a.y);
            var z = -CopySign(a.x, a.z) - CopySign(a.y, a.z);

            return new float4(x, y, z, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CopySign(float a, float b)
        {
            return (b >= 0 ? 1f : -1f) * math.abs(a);
        }
    }
}