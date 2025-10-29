using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathFloor : BehaviourEngineNode
    {
        public MathFloor(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.floor(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.floor(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.floor(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.floor(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(floor(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(floor(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(floor(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 floor(float2x2 a)
        {
            return new float2x2(math.floor(a.c0), math.floor(a.c1));
        }

        private static float3x3 floor(float3x3 a)
        {
            return new float3x3(math.floor(a.c0), math.floor(a.c1), math.floor(a.c2));
        }

        private static float4x4 floor(float4x4 a)
        {
            return new float4x4(math.floor(a.c0), math.floor(a.c1), math.floor(a.c2), math.floor(a.c3));
        }
    }
}