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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float3x3)
                throw new InvalidOperationException("Input A is not a 3x3 matrix!");

            return id switch
            {
                "0" => Variant.FromFloat(a.Float3x3.c0.x),
                "1" => Variant.FromFloat(a.Float3x3.c0.y),
                "2" => Variant.FromFloat(a.Float3x3.c0.z),
                "3" => Variant.FromFloat(a.Float3x3.c1.x),
                "4" => Variant.FromFloat(a.Float3x3.c1.y),
                "5" => Variant.FromFloat(a.Float3x3.c1.z),
                "6" => Variant.FromFloat(a.Float3x3.c2.x),
                "7" => Variant.FromFloat(a.Float3x3.c2.y),
                "8" => Variant.FromFloat(a.Float3x3.c2.z),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

    }
}