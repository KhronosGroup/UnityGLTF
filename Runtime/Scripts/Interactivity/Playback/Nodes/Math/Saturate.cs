using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSaturate : BehaviourEngineNode
    {
        public MathSaturate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(math.saturate(aProp.value)),
                Property<float2> aProp => new Property<float2>(math.saturate(aProp.value)),
                Property<float3> aProp => new Property<float3>(math.saturate(aProp.value)),
                Property<float4> aProp => new Property<float4>(math.saturate(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}