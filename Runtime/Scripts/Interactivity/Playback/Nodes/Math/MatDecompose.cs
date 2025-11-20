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

            var translation = float3.zero;
            var rotation = new float4(0f, 0f, 0f, 1f);
            var scale = new float3(1f, 1f, 1f);
            var isValid = IsMatrixDecomposable(mProp.value);

            if(isValid)
                mProp.value.Decompose(out translation, out rotation, out scale);

            return id switch
            {
                ConstStrings.TRANSLATION => new Property<float3>(translation),
                ConstStrings.ROTATION => new Property<float4>(rotation),
                ConstStrings.SCALE => new Property<float3>(scale),
                ConstStrings.IS_VALID => new Property<bool>(isValid),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        private static bool IsMatrixDecomposable(in float4x4 m)
        {
            if (!LastRowIsValid(m))
                return false;

            if (!ScaleIsFinite(m, out var s))
                return false;

            if (!ScaledDeterminateIsOne(m, s))
                return false;

            return true;
        }

        private static bool LastRowIsValid(in float4x4 m)
        {
            return m.c0.w == 0f && m.c1.w == 0f && m.c2.w == 0f && m.c3.w == 1f;
        }

        private static bool ScaleIsFinite(in float4x4 m, out float3 s)
        {
            s = float3.zero;
            s.x = math.sqrt(m.c0.x * m.c0.x + m.c0.y * m.c0.y + m.c0.z * m.c0.z);

            if (InfiniteZeroOrNaN(s.x))
                return false;

            s.y = math.sqrt(m.c1.x * m.c1.x + m.c1.y * m.c1.y + m.c1.z * m.c1.z);

            if (InfiniteZeroOrNaN(s.y))
                return false;

            s.z = math.sqrt(m.c2.x * m.c2.x + m.c2.y * m.c2.y + m.c2.z * m.c2.z);

            if (InfiniteZeroOrNaN(s.z))
                return false;

            return true;
        }

        private static bool InfiniteZeroOrNaN(float v)
        {
            return v == 0 || math.isinf(v) || math.isnan(v);
        }

        private static bool ScaledDeterminateIsOne(in float4x4 m, in float3 s)
        {
            var b = new float3x3(new float4x4(m.c0 / s.x, m.c1 / s.y, m.c2 / s.z, m.c3));

            return Mathf.Approximately(math.abs(math.determinant(b)), 1f);
        }
    }
}