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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float3> aProp when b is Property<float3> bProp => new Property<float4>(FromDirections(aProp.value, bProp.value)),
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