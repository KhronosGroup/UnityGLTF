using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathTranspose : BehaviourEngineNode
    {
        public MathTranspose(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float2x2> aProp => new Property<float2x2>(math.transpose(aProp.value)),
                Property<float3x3> aProp => new Property<float3x3>(math.transpose(aProp.value)),
                Property<float4x4> aProp => new Property<float4x4>(math.transpose(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}