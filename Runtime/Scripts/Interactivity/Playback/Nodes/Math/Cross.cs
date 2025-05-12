using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathCross : BehaviourEngineNode
    {
        public MathCross(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float3> aProp when b is Property<float3> bProp => new Property<float3>(math.cross(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}