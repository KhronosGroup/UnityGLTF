using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathDot : BehaviourEngineNode
    {
        public MathDot(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float2> aProp when b is Property<float2> bProp => new Property<float>(math.dot(aProp.value, bProp.value)),
                Property<float3> aProp when b is Property<float3> bProp => new Property<float>(math.dot(aProp.value, bProp.value)),
                Property<float4> aProp when b is Property<float4> bProp => new Property<float>(math.dot(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}