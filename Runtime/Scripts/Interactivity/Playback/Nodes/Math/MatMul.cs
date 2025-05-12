using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMatMul : BehaviourEngineNode
    {
        public MathMatMul(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float2x2> aProp when b is Property<float2x2> bProp => new Property<float2x2>(math.mul(aProp.value, bProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp => new Property<float3x3>(math.mul(aProp.value, bProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp => new Property<float4x4>(math.mul(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}