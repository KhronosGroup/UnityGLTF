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

            var m = mProp.value;

            if (!LastRowIsValid(m)) return DefaultOutputValues(id);

            var sx = math.length(m.c0.xyz);
            var sy = math.length(m.c1.xyz);
            var sz = math.length(m.c2.xyz);
            float3 scale = new float3(sx, sy, sz);

            if (!ScaleIsFinite(scale)) return DefaultOutputValues(id);
            
            var B = new float3x3(m.c0.xyz / sx, m.c1.xyz / sy, m.c2.xyz / sz);
            var detB = math.determinant(B);
            
            if(!ScaledDeterminateIsOne(detB)) return DefaultOutputValues(id);

            var translation = m.c3.xyz;

            if (detB < 0f)
            {
                scale *= -1f;
                B *= -1f;
            }

            var rotation = new quaternion(B).value;

            return id switch
            {
                ConstStrings.TRANSLATION => new Property<float3>(translation),
                ConstStrings.ROTATION => new Property<float4>(rotation),
                ConstStrings.SCALE => new Property<float3>(scale),
                ConstStrings.IS_VALID => new Property<bool>(true),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        private static IProperty DefaultOutputValues(string id)
        {
            return id switch
            {
                ConstStrings.TRANSLATION => new Property<float3>(float3.zero),
                ConstStrings.ROTATION => new Property<float4>(new float4(0f, 0f, 0f, 1f)),
                ConstStrings.SCALE => new Property<float3>(new float3(1f, 1f, 1f)),
                ConstStrings.IS_VALID => new Property<bool>(false),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        private static bool LastRowIsValid(in float4x4 m)
        {
            return m.c0.w == 0f && m.c1.w == 0f && m.c2.w == 0f && Mathf.Approximately(m.c3.w, 1f);
        }

        private static bool ScaleIsFinite(float3 s)
        {
            if (InfiniteZeroOrNaN(s.x))
                return false;

            if (InfiniteZeroOrNaN(s.y))
                return false;

            if (InfiniteZeroOrNaN(s.z))
                return false;

            return true;
        }

        private static bool InfiniteZeroOrNaN(float v)
        {
            return v == 0f || math.isinf(v) || math.isnan(v);
        }

        private static bool ScaledDeterminateIsOne(float detB)
        {
            return Mathf.Approximately(math.abs(detB), 1f);
        }
    }
}