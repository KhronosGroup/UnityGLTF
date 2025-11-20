using System;
using Unity.Mathematics;
using UnityEngine;


namespace UnityGLTF.Interactivity.Playback
{
    public class MathCbrt : BehaviourEngineNode
    {
        public MathCbrt(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float> floatProp => new Property<float>(MathF.Cbrt(floatProp.value)),
                Property<float2> float2Prop => new Property<float2>(Cbrt(float2Prop.value)),
                Property<float3> float3Prop => new Property<float3>(Cbrt(float3Prop.value)),
                Property<float4> float4Prop => new Property<float4>(Cbrt(float4Prop.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2 Cbrt(float2 v)
        {
            return new float2(MathF.Cbrt(v.x), MathF.Cbrt(v.y));
        }

        private static float3 Cbrt(float3 v)
        {
            return new float3(MathF.Cbrt(v.x), MathF.Cbrt(v.y), MathF.Cbrt(v.z));
        }

        private static float4 Cbrt(float4 v)
        {
            return new float4(MathF.Cbrt(v.x), MathF.Cbrt(v.y), MathF.Cbrt(v.z), MathF.Cbrt(v.w));
        }
    }
}