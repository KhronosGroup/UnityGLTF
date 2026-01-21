using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathASinH : BehaviourEngineNode
    {
        public MathASinH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> floatProp => new Property<float>(asinh(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(asinh(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(asinh(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(asinh(float4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private float asinh(float x)
        {
            // ln(x + sqrt(x^2 + 1))
            return math.log(x + math.sqrt(x * x + 1));
        }

        private float2 asinh(float2 x)
        {
            return math.log(x + math.sqrt(x * x + 1));
        }

        private float3 asinh(float3 x)
        {
            return math.log(x + math.sqrt(x * x + 1));
        }

        private float4 asinh(float4 x)
        {
            return math.log(x + math.sqrt(x * x + 1));
        }
    }
}