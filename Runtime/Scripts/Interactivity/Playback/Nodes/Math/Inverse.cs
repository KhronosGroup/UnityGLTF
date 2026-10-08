using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathInverse : BehaviourEngineNode
    {
        public MathInverse(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out Variant a);

            bool isValid;

            Variant prop = a.type switch
            {
                VariantType.Float2x2 => Variant.FromFloat2x2(Inverse(a.Float2x2, out isValid)),
                VariantType.Float3x3 => Variant.FromFloat3x3(Inverse(a.Float3x3, out isValid)),
                VariantType.Float4x4 => Variant.FromFloat4x4(Inverse(a.Float4x4, out isValid)),
                _ => throw new InvalidOperationException("No supported type found."),
            };

            return id switch
            {
                ConstStrings.VALUE => prop,
                ConstStrings.IS_VALID => Variant.FromBool(isValid),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        private static float2x2 Inverse(float2x2 m, out bool isValid)
        {
            var det = math.determinant(m);
            if (det == 0f || float.IsInfinity(det) || float.IsNaN(det))
            {
                isValid = false;
                return float2x2.zero;
            }

            var inverse = math.inverse(m);
            isValid = true;

            return inverse;
        }

        private static float3x3 Inverse(float3x3 m, out bool isValid)
        {
            var det = math.determinant(m);
            if (det == 0f || float.IsInfinity(det) || float.IsNaN(det))
            {
                isValid = false;
                return float3x3.zero;
            }

            var inverse = math.inverse(m);
            isValid = true;

            return inverse;
        }

        private static float4x4 Inverse(float4x4 m, out bool isValid)
        {
            var det = math.determinant(m);
            if (det == 0f || float.IsInfinity(det) || float.IsNaN(det))
            {
                isValid = false;
                return float4x4.zero;
            }

            var inverse = math.inverse(m);
            isValid = true;

            return inverse;
        }
    }
}