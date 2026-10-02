using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathNormalize : BehaviourEngineNode
    {
        public MathNormalize(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            // A zero, NaN or infinite length gives isValid = false and a zero vector.
            bool valid;
            IProperty value = a switch
            {
                Property<float2> p => new Property<float2>(Normalize(p.value, math.length(p.value), out valid)),
                Property<float3> p => new Property<float3>(Normalize(p.value, math.length(p.value), out valid)),
                Property<float4> p => new Property<float4>(Normalize(p.value, math.length(p.value), out valid)),
                _ => throw new InvalidOperationException("No supported type found."),
            };

            return id switch
            {
                ConstStrings.VALUE => value,
                ConstStrings.IS_VALID => new Property<bool>(valid),
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
