using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract2x2 : BehaviourEngineNode
    {
        public MathExtract2x2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float2x2> property)
                throw new InvalidOperationException("Input A is not a float2x2!");

            return id switch
            {
                "0" => new Property<float>(property.value.c0.x),
                "1" => new Property<float>(property.value.c0.y),
                "2" => new Property<float>(property.value.c1.x),
                "3" => new Property<float>(property.value.c1.y),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }
    }
}