using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRem : BehaviourEngineNode
    {
        public MathRem(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<int> aInt when b is Property<int> bInt => new Property<int>(aInt.value % bInt.value),
                Property<float> aFloat when b is Property<float> bFloat => new Property<float>(aFloat.value % bFloat.value),
                Property<float2> aVec2 when b is Property<float2> bVec2 => new Property<float2>((float2)aVec2.value % bVec2.value),
                Property<float3> aVec3 when b is Property<float3> bVec3 => new Property<float3>((float3)aVec3.value % bVec3.value),
                Property<float4> aVec4 when b is Property<float4> bVec4 => new Property<float4>((float4)aVec4.value % bVec4.value),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }
    }
}