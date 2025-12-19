using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathATan : BehaviourEngineNode
    {
        public MathATan(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> floatProp => new Property<float>(math.atan(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(math.atan(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(math.atan(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(math.atan(float4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}