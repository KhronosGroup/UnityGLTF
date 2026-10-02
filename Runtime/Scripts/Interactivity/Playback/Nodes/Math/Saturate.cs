using System;
using Unity.Mathematics;
using UnityEngine;
using static UnityGLTF.Interactivity.Playback.MathMax;
using static UnityGLTF.Interactivity.Playback.MathMin;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSaturate : BehaviourEngineNode
    {
        public MathSaturate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        // Spec: min(max(a, 0), 1) using math/min and math/max, so NaN propagates (math.saturate returns 0).
        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> aProp => new Property<float>(Min(Max(aProp.value, 0f), 1f)),
                Property<float2> aProp => new Property<float2>(Min(Max(aProp.value, float2.zero), new float2(1f))),
                Property<float3> aProp => new Property<float3>(Min(Max(aProp.value, float3.zero), new float3(1f))),
                Property<float4> aProp => new Property<float4>(Min(Max(aProp.value, float4.zero), new float4(1f))),
                Property<float2x2> aProp => new Property<float2x2>(Min(Max(aProp.value, float2x2.zero), new float2x2(1f))),
                Property<float3x3> aProp => new Property<float3x3>(Min(Max(aProp.value, float3x3.zero), new float3x3(1f))),
                Property<float4x4> aProp => new Property<float4x4>(Min(Max(aProp.value, float4x4.zero), new float4x4(1f))),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }
    }
}
