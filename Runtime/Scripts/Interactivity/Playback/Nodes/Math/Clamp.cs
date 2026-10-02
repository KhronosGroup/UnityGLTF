using System;
using Unity.Mathematics;
using UnityEngine;
using static UnityGLTF.Interactivity.Playback.MathMax;
using static UnityGLTF.Interactivity.Playback.MathMin;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathClamp : BehaviourEngineNode
    {
        public MathClamp(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        // Spec: min(max(a, min(b, c)), max(b, c)), which tolerates b > c and propagates NaN (math.clamp does neither).
        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);

            return a switch
            {
                Property<int> aProp when b is Property<int> bProp && c is Property<int> cProp => new Property<int>(math.min(math.max(aProp.value, math.min(bProp.value, cProp.value)), math.max(bProp.value, cProp.value))),
                Property<float> aProp when b is Property<float> bProp && c is Property<float> cProp => new Property<float>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float2> aProp when b is Property<float2> bProp && c is Property<float2> cProp => new Property<float2>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float3> aProp when b is Property<float3> bProp && c is Property<float3> cProp => new Property<float3>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float4> aProp when b is Property<float4> bProp && c is Property<float4> cProp => new Property<float4>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float2x2> aProp when b is Property<float2x2> bProp && c is Property<float2x2> cProp => new Property<float2x2>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float3x3> aProp when b is Property<float3x3> bProp && c is Property<float3x3> cProp => new Property<float3x3>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                Property<float4x4> aProp when b is Property<float4x4> bProp && c is Property<float4x4> cProp => new Property<float4x4>(Min(Max(aProp.value, Min(bProp.value, cProp.value)), Max(bProp.value, cProp.value))),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}
