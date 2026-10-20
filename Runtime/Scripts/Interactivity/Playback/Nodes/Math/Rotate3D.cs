using System;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRotate3D : BehaviourEngineNode
    {
        public MathRotate3D(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.ROTATION, out Variant b);

            return a.type switch
            {
                VariantType.Float3 when b.type == VariantType.Float4 => Variant.FromFloat3(rotate(a.Float3, b.Float4)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float3 rotate(float3 a, float4 b)
        {
            var b3 = b.xyz;
            var b3xa = math.cross(b3, a);
            // Is this equivalent to math.mul(b.ToQuaternion(), a)?
            // I have no idea so let's be safe and use the eq from the spec.
            return a + 2f * (math.cross(b3, b3xa) + b.w * b3xa);
        }
    }
}