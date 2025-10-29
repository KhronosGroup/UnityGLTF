using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCeil : BehaviourEngineNode
    {
        public MathCeil(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.ceil(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.ceil(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.ceil(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.ceil(aProp.value)),
                Property<float2x2> aProp => new Property<float2x2>(ceil(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(ceil(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(ceil(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 ceil(float2x2 a)
        {
            return new float2x2(math.ceil(a.c0), math.ceil(a.c1));
        }

        private static float3x3 ceil(float3x3 a)
        {
            return new float3x3(math.ceil(a.c0), math.ceil(a.c1), math.ceil(a.c2));
        }

        private static float4x4 ceil(float4x4 a)
        {
            return new float4x4(math.ceil(a.c0), math.ceil(a.c1), math.ceil(a.c2), math.ceil(a.c3));
        }
    }

}