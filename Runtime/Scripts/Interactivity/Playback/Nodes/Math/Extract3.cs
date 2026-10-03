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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float3)
                throw new InvalidOperationException("Input A is not a float3!");

            switch (id)
            {
                case "0":
                    return Variant.FromFloat(a.Float3.x);

                case "1":
                    return Variant.FromFloat(a.Float3.y);

                case "2":
                    return Variant.FromFloat(a.Float3.z);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}