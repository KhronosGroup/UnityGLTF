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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float2)
                throw new InvalidOperationException("Input A is not a float2!");

            switch (id)
            {
                case "0":
                    return Variant.FromFloat(a.Float2.x);

                case "1":
                    return Variant.FromFloat(a.Float2.y);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}