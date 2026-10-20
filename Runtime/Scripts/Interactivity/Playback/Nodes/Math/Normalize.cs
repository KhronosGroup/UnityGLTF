using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNormalize : BehaviourEngineNode
    {
        public MathNormalize(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            // A zero, NaN or infinite length gives isValid = false and a zero vector.
            bool valid;
            Variant value = a.type switch
            {
                VariantType.Float2 => Variant.FromFloat2(Normalize(a.Float2, math.length(a.Float2), out valid)),
                VariantType.Float3 => Variant.FromFloat3(Normalize(a.Float3, math.length(a.Float3), out valid)),
                VariantType.Float4 => Variant.FromFloat4(Normalize(a.Float4, math.length(a.Float4), out valid)),
                _ => throw new InvalidOperationException("No supported type found."),
            };

            return id switch
            {
                ConstStrings.VALUE => value,
                ConstStrings.IS_VALID => Variant.FromBool(valid),
                _ => throw new InvalidOperationException($"Socket {id} is not valid for this node!"),
            };
        }

        private static bool IsUsableLength(float length) => length > 0f && !float.IsInfinity(length);

        private static float2 Normalize(float2 v, float length, out bool valid)
        {
            valid = IsUsableLength(length);
            return valid ? v / length : float2.zero;
        }

        private static float3 Normalize(float3 v, float length, out bool valid)
        {
            valid = IsUsableLength(length);
            return valid ? v / length : float3.zero;
        }

        private static float4 Normalize(float4 v, float length, out bool valid)
        {
            valid = IsUsableLength(length);
            return valid ? v / length : float4.zero;
        }
    }
}
