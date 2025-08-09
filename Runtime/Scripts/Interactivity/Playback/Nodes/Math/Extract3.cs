using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract3 : BehaviourEngineNode
    {
        public MathExtract3(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float3> property)
                throw new InvalidOperationException("Input A is not a float3!");

            switch (id)
            {
                case "0":
                    return new Property<float>(property.value.x);

                case "1":
                    return new Property<float>(property.value.y);

                case "2":
                    return new Property<float>(property.value.z);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}