using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathLe : BehaviourEngineNode
    {
        public MathLe(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);
            TryEvaluateValue(ConstStrings.B, out Variant b);

            return a.type switch
            {
                VariantType.Int when b.type == VariantType.Int => Variant.FromBool(a.Int <= b.Int),
                VariantType.Float when b.type == VariantType.Float => Variant.FromBool(a.Float <= b.Float),
                // TODO: Support these types?
                //Property<float2> aVec2 when b.type == VariantType.Float2 => Variant.FromBool(aVec2.value > b.Float2),
                //Property<float3> aVec3 when b.type == VariantType.Float3 => Variant.FromBool(aVec3.value > b.Float3),
                //Property<float4> aVec4 when b.type == VariantType.Float4 => Variant.FromBool(aVec4.value > b.Float4),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}