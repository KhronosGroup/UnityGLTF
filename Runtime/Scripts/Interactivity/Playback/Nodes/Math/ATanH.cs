using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathATanH : BehaviourEngineNode
    {
        public MathATanH(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> floatProp => new Property<float>(ATanH(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(ATanH(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(ATanH(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(ATanH(float4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private float ATanH(float x)
        {
            // 0.5 * ln((1+x)/(1-x))
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float2 ATanH(float2 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float3 ATanH(float3 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }

        private float4 ATanH(float4 x)
        {
            return 0.5f * math.log((1 + x) / (1 - x));
        }
    }
}