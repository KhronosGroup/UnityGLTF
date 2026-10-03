using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathExtract4 : BehaviourEngineNode
    {
        public MathExtract4(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float4)
                throw new InvalidOperationException($"Input A is not a float4! It is {a.GetTypeSignature()}");

            switch (id)
            {
                case "0":
                    return Variant.FromFloat(a.Float4.x);

                case "1":
                    return Variant.FromFloat(a.Float4.y);

                case "2":
                    return Variant.FromFloat(a.Float4.z);

                case "3":
                    return Variant.FromFloat(a.Float4.w);
            }

            throw new InvalidOperationException($"Socket {id} is not valid for this node!");
        }
    }
}