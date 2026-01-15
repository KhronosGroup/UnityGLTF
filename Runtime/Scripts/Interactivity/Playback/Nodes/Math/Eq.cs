using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathEq : BehaviourEngineNode
    {
        public MathEq(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<bool> aProp when b is Property<bool> bProp => new Property<bool>(aProp.value == bProp.value),
                Property<int> aInt when b is Property<int> bInt => new Property<bool>(aInt.value == bInt.value),
                Property<float> aFloat when b is Property<float> bFloat => new Property<bool>(eq(aFloat.value,bFloat.value)),
                Property<float2> pA when b is Property<float2> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                Property<float3> pA when b is Property<float3> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                Property<float4> pA when b is Property<float4> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                Property<float2x2> pA when b is Property<float2x2> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                Property<float3x3> pA when b is Property<float3x3> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                Property<float4x4> pA when b is Property<float4x4> pB => new Property<bool>(AllEqual(pA.value, pB.value)),
                _ => throw new InvalidOperationException($"No supported type found or input types did not match. Types were A: {a.GetTypeSignature()}, B: {b.GetTypeSignature()}"),
            };
        }

        private static bool AllEqual(float2 a, float2 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y);
        }

        private static bool AllEqual(float3 a, float3 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y) && eq(a.z, b.z);
        }

        private static bool AllEqual(float4 a, float4 b)
        {
            return eq(a.x, b.x) && eq(a.y, b.y) && eq(a.z, b.z) && eq(a.w, b.w);
        }

        private static bool AllEqual(float2x2 a, float2x2 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y);
        }

        private static bool AllEqual(float3x3 a, float3x3 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) && eq(a.c0.z, b.c0.z) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y) && eq(a.c1.z, b.c1.z) &&
                   eq(a.c2.x, b.c2.x) && eq(a.c2.y, b.c2.y) && eq(a.c2.z, b.c2.z);
        }

        private static bool AllEqual(float4x4 a, float4x4 b)
        {
            return eq(a.c0.x, b.c0.x) && eq(a.c0.y, b.c0.y) && eq(a.c0.z, b.c0.z) && eq(a.c0.w, b.c0.w) &&
                   eq(a.c1.x, b.c1.x) && eq(a.c1.y, b.c1.y) && eq(a.c1.z, b.c1.z) && eq(a.c1.w, b.c1.w) &&
                   eq(a.c2.x, b.c2.x) && eq(a.c2.y, b.c2.y) && eq(a.c2.z, b.c2.z) && eq(a.c2.w, b.c2.w) &&
                   eq(a.c3.x, b.c3.x) && eq(a.c3.y, b.c3.y) && eq(a.c3.z, b.c3.z) && eq(a.c3.w, b.c3.w);
        }

        private static bool eq(float a, float b)
        {
            // IEEE standard used for this spec says that inf==inf so we have to make sure that's true.
            return Mathf.Approximately(a,b) || (float.IsInfinity(a) && float.IsInfinity(b));
        }
    }
}