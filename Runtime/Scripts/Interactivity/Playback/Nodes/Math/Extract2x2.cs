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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float2x2)
                throw new InvalidOperationException("Input A is not a float2x2!");

            return id switch
            {
                "0" => Variant.FromFloat(a.Float2x2.c0.x),
                "1" => Variant.FromFloat(a.Float2x2.c0.y),
                "2" => Variant.FromFloat(a.Float2x2.c1.x),
                "3" => Variant.FromFloat(a.Float2x2.c1.y),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }
    }
}