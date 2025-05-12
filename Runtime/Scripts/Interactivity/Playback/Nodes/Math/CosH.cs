using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCosH : BehaviourEngineNode
    {
        public MathCosH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> floatProp => new Property<float>(math.cosh(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(math.cosh(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(math.cosh(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(math.cosh(float4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}