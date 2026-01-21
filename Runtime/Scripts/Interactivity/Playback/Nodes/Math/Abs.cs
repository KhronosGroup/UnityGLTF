using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathAbs : BehaviourEngineNode
    {
        public MathAbs(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> intProp => new Property<int>(math.abs(intProp.value)),
                Property<float> floatProp => new Property<float>(math.abs(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(math.abs(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(math.abs(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(math.abs(float4Prop.value)),
                Property<float2x2> float2x2Prop => new Property<float2x2>(abs(float2x2Prop.value)),
                Property<float3x3> float3x3Prop => new Property<float3x3>(abs(float3x3Prop.value)),
                Property<float4x4> float4x4Prop => new Property<float4x4>(abs(float4x4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 abs(float2x2 a)
        {
            return new float2x2(math.abs(a.c0), math.abs(a.c1));
        }

        private static float3x3 abs(float3x3 a)
        {
            return new float3x3(math.abs(a.c0), math.abs(a.c1), math.abs(a.c2));
        }

        private static float4x4 abs(float4x4 a)
        {
            return new float4x4(math.abs(a.c0), math.abs(a.c1), math.abs(a.c2), math.abs(a.c3));
        }
    }
}