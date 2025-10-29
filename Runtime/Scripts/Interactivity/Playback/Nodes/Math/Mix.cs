using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMix : BehaviourEngineNode
    {
        public MathMix(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);

            return a switch
            {
                Property<float> aProp when b is Property<float> bProp && c is Property<float> cProp => new Property<float>(math.lerp(aProp.value, bProp.value, cProp.value)),
                Property<float2> aProp when b is Property<float2> bProp && c is Property<float2> cProp => new Property<float2>(math.lerp(aProp.value, bProp.value, cProp.value)),
                Property<float3> aProp when b is Property<float3> bProp && c is Property<float3> cProp => new Property<float3>(math.lerp(aProp.value, bProp.value, cProp.value)),
                Property<float4> aProp when b is Property<float4> bProp && c is Property<float4> cProp => new Property<float4>(math.lerp(aProp.value, bProp.value, cProp.value)),
                Property<float2x2> aProp when b is Property<float2x2> bProp && c is Property<float2x2> cProp =>new Property<float2x2>(lerp(aProp.value, bProp.value, cProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp && c is Property<float3x3> cProp =>new Property<float3x3>(lerp(aProp.value, bProp.value, cProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp && c is Property<float4x4> cProp =>new Property<float4x4>(lerp(aProp.value, bProp.value, cProp.value)),

                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        private static float2x2 lerp(float2x2 a, float2x2 b, float2x2 t)
        {
            return new float2x2(math.lerp(a.c0, b.c0, t.c0), math.lerp(a.c1, b.c1, t.c1));
        }

        private static float3x3 lerp(float3x3 a, float3x3 b, float3x3 t)
        {
            return new float3x3(
                math.lerp(a.c0, b.c0, t.c0),
                math.lerp(a.c1, b.c1, t.c1),
                math.lerp(a.c2, b.c2, t.c2)
            );
        }

        private static float4x4 lerp(float4x4 a, float4x4 b, float4x4 t)
        {
            return new float4x4(
                math.lerp(a.c0, b.c0, t.c0),
                math.lerp(a.c1, b.c1, t.c1),
                math.lerp(a.c2, b.c2, t.c2),
                math.lerp(a.c3, b.c3, t.c3)
            );
        }
    }
}