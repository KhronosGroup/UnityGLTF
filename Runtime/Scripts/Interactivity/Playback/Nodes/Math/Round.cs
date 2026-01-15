using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRound : BehaviourEngineNode
    {
        public MathRound(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(round(aProp.value)),
                Property<float2> aProp => new Property<float2>(round(aProp.value)),
                Property<float3> aProp => new Property<float3>(round(aProp.value)),
                Property<float4> aProp => new Property<float4>(round(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(round(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(round(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(round(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 round(float2x2 a)
        {
            return new float2x2(round(a.c0), round(a.c1));
        }

        private static float3x3 round(float3x3 a)
        {
            return new float3x3(round(a.c0), round(a.c1), round(a.c2));
        }

        private static float4x4 round(float4x4 a)
        {
            return new float4x4(round(a.c0), round(a.c1), round(a.c2), round(a.c3));
        }

        private static float2 round(float2 a)
        {
            return new float2(round(a.x), round(a.y));
        }

        private static float3 round(float3 a)
        {
            return new float3(round(a.x), round(a.y), round(a.z));
        }

        private static float4 round(float4 a)
        {
            return new float4(round(a.x), round(a.y), round(a.z), round(a.w));
        }

        private static float round(float f)
        {
            return (float)System.Math.Round(f, MidpointRounding.AwayFromZero);
        }
    }

}