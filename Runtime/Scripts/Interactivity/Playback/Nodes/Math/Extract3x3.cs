using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract3x3 : BehaviourEngineNode
    {
        public MathExtract3x3(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float3x3> property)
                throw new InvalidOperationException("Input A is not a 3x3 matrix!");

            return id switch
            {
                "0" => new Property<float>(property.value.c0.x),
                "1" => new Property<float>(property.value.c0.y),
                "2" => new Property<float>(property.value.c0.z),
                "3" => new Property<float>(property.value.c1.x),
                "4" => new Property<float>(property.value.c1.y),
                "5" => new Property<float>(property.value.c1.z),
                "6" => new Property<float>(property.value.c2.x),
                "7" => new Property<float>(property.value.c2.y),
                "8" => new Property<float>(property.value.c2.z),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

    }
}