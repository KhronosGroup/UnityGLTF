using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathLength : BehaviourEngineNode
    {
        public MathLength(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float2> aProp => new Property<float>(math.length(aProp.value)),
                Property<float3> aProp => new Property<float>(math.length(aProp.value)),
                Property<float4> aProp => new Property<float>(math.length(aProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}