using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract2 : BehaviourEngineNode
    {
        public MathExtract2(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            if (a is not Property<float2> property)
                throw new InvalidOperationException("Input A is not a float2!");

            switch (id)
            {
                case "0":
                    return new Property<float>(property.value.x);

                case "1":
                    return new Property<float>(property.value.y);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}