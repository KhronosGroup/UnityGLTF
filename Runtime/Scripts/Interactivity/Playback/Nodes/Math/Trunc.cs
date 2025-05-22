using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTrunc : BehaviourEngineNode
    {
        public MathTrunc(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> prop => new Property<float>(math.trunc(prop.value)),
                Property<float2> prop => new Property<float2>(math.trunc(prop.value)),
                Property<float3> prop => new Property<float3>(math.trunc(prop.value)),
                Property<float4> prop => new Property<float4>(math.trunc(prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}