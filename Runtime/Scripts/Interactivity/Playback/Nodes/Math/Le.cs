using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathLe : BehaviourEngineNode
    {
        public MathLe(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<int> aInt when b is Property<int> bInt => new Property<bool>(aInt.value <= bInt.value),
                Property<float> aFloat when b is Property<float> bFloat => new Property<bool>(aFloat.value <= bFloat.value),
                // TODO: Support these types?
                //Property<float2> aVec2 when b is Property<float2> bVec2 => new Property<bool>(aVec2.value > bVec2.value),
                //Property<float3> aVec3 when b is Property<float3> bVec3 => new Property<bool>(aVec3.value > bVec3.value),
                //Property<float4> aVec4 when b is Property<float4> bVec4 => new Property<bool>(aVec4.value > bVec4.value),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}