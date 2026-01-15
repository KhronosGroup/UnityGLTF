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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            bool isValid;

            IProperty prop = a switch
            {
                Property<float2x2> aProp => new Property<float2x2>(Inverse(aProp.value, out isValid)),
                Property<float3x3> aProp => new Property<float3x3>(Inverse(aProp.value, out isValid)),
                Property<float4x4> aProp => new Property<float4x4>(Inverse(aProp.value, out isValid)),
                _ => throw new InvalidOperationException("No supported type found."),
            };

            return id switch
            {
                ConstStrings.VALUE => prop,
                ConstStrings.IS_VALID => new Property<bool>(isValid),
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