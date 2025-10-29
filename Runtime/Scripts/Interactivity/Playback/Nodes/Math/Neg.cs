using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNeg : BehaviourEngineNode
    {
        public MathNeg(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<int> aProp => new Property<int>(-aProp.value),
                Property<float> aProp => new Property<float>(-aProp.value),
                Property<float2> aProp => new Property<float2>(-aProp.value),
                Property<float3> aProp => new Property<float3>(-aProp.value),
                Property<float4> aProp => new Property<float4>(-aProp.value),
                Property<float2x2> aProp => new Property<float2x2>(-aProp.value),
                Property<float3x3> aProp => new Property<float3x3>(-aProp.value),
                Property<float4x4> aProp => new Property<float4x4>(-aProp.value),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}