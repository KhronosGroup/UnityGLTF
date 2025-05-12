using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathFract : BehaviourEngineNode
    {
        public MathFract(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> prop => new Property<float>(math.frac(prop.value)),
                Property<float2> prop => new Property<float2>(math.frac(prop.value)),
                Property<float3> prop => new Property<float3>(math.frac(prop.value)),
                Property<float4> prop => new Property<float4>(math.frac(prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}