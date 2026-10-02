using System;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMatDecompose : BehaviourEngineNode
    {
        public MathMatDecompose(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float4x4> mProp)
                throw new InvalidOperationException($"Type of value a must be Matrix4x4 but a {a.GetTypeSignature()} was passed in!");

            Decompose(mProp.value, out var translation, out var rotation, out var scale);

            return id switch
            {
                ConstStrings.TRANSLATION => new Property<float3>(translation),
                ConstStrings.ROTATION => new Property<float4>(rotation),
                ConstStrings.SCALE => new Property<float3>(scale),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        // Follows the spec steps: the fourth row is ignored, degenerate scales give an identity rotation,
        // and shear is left in place.
        public static void Decompose(in float4x4 m, out float3 translation, out float4 rotation, out float3 scale)
        {
            translation = m.c3.xyz;

            var sx = math.length(m.c0.xyz);
            var sy = math.length(m.c1.xyz);
            var sz = math.length(m.c2.xyz);
            scale = new float3(sx, sy, sz);

            if (InfiniteZeroOrNaN(sx) || InfiniteZeroOrNaN(sy) || InfiniteZeroOrNaN(sz))
            {
                rotation = new float4(0f, 0f, 0f, 1f);
                return;
            }

            var B = new float3x3(m.c0.xyz / sx, m.c1.xyz / sy, m.c2.xyz / sz);

            if (math.determinant(B) < 0f)
            {
                scale.x = -scale.x;
                B.c0 = -B.c0;
            }

            rotation = math.normalize(new quaternion(B).value);
        }

        private static bool InfiniteZeroOrNaN(float v)
        {
            return v == 0f || math.isinf(v) || math.isnan(v);
        }
    }
}
