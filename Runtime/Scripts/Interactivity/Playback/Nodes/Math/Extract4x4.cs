using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract4x4 : BehaviourEngineNode
    {
        public MathExtract4x4(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float4x4> property)
                throw new InvalidOperationException("Input A is not a 4x4 matrix!");

            return id switch
            {
                "0" => new Property<float>(property.value.c0.x),
                "1" => new Property<float>(property.value.c0.y),
                "2" => new Property<float>(property.value.c0.z),
                "3" => new Property<float>(property.value.c0.w),
                "4" => new Property<float>(property.value.c1.x),
                "5" => new Property<float>(property.value.c1.y),
                "6" => new Property<float>(property.value.c1.z),
                "7" => new Property<float>(property.value.c1.w),
                "8" => new Property<float>(property.value.c2.x),
                "9" => new Property<float>(property.value.c2.y),
                "10" => new Property<float>(property.value.c2.z),
                "11" => new Property<float>(property.value.c2.w),
                "12" => new Property<float>(property.value.c3.x),
                "13" => new Property<float>(property.value.c3.y),
                "14" => new Property<float>(property.value.c3.z),
                "15" => new Property<float>(property.value.c3.w),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

    }
}