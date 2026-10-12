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

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            if (a.type != VariantType.Float4x4)
                throw new InvalidOperationException("Input A is not a 4x4 matrix!");

            return id switch
            {
                "0" => Variant.FromFloat(a.Float4x4.c0.x),
                "1" => Variant.FromFloat(a.Float4x4.c0.y),
                "2" => Variant.FromFloat(a.Float4x4.c0.z),
                "3" => Variant.FromFloat(a.Float4x4.c0.w),
                "4" => Variant.FromFloat(a.Float4x4.c1.x),
                "5" => Variant.FromFloat(a.Float4x4.c1.y),
                "6" => Variant.FromFloat(a.Float4x4.c1.z),
                "7" => Variant.FromFloat(a.Float4x4.c1.w),
                "8" => Variant.FromFloat(a.Float4x4.c2.x),
                "9" => Variant.FromFloat(a.Float4x4.c2.y),
                "10" => Variant.FromFloat(a.Float4x4.c2.z),
                "11" => Variant.FromFloat(a.Float4x4.c2.w),
                "12" => Variant.FromFloat(a.Float4x4.c3.x),
                "13" => Variant.FromFloat(a.Float4x4.c3.y),
                "14" => Variant.FromFloat(a.Float4x4.c3.z),
                "15" => Variant.FromFloat(a.Float4x4.c3.w),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

    }
}