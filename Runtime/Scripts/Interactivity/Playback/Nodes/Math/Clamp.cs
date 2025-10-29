using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathClamp : BehaviourEngineNode
    {
        public MathClamp(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out IProperty c);

            // if (b is not Property<float> bProp)
            //     throw new InvalidOperationException($"B must be a float value.");

            // if (c is not Property<float> cProp)
            //     throw new InvalidOperationException($"C must be a float value.");

            return a switch
            {
                Property<float> aProp when b is Property<float> bProp && c is Property<float> cProp => new Property<float>(math.clamp(aProp.value, bProp.value, cProp.value)),
                Property<float2> aProp when b is Property<float2> bProp && c is Property<float2> cProp  => new Property<float2>(math.clamp(aProp.value, bProp.value, cProp.value)),
                Property<float3> aProp when b is Property<float3> bProp && c is Property<float3> cProp => new Property<float3>(math.clamp(aProp.value, bProp.value, cProp.value)),
                Property<float4> aProp when b is Property<float4> bProp && c is Property<float4> cProp => new Property<float4>(math.clamp(aProp.value, bProp.value, cProp.value)),
                Property<float2x2> aProp when b is Property<float2x2> bProp && c is Property<float2x2> cProp => new Property<float2x2>(clamp(aProp.value, bProp.value, cProp.value)),
                Property<float3x3> aProp when b is Property<float3x3> bProp && c is Property<float3x3> cProp => new Property<float3x3>(clamp(aProp.value, bProp.value, cProp.value)),
                Property<float4x4> aProp when b is Property<float4x4> bProp && c is Property<float4x4> cProp => new Property<float4x4>(clamp(aProp.value, bProp.value, cProp.value)),
                _ => throw new InvalidOperationException("No supported type found."),
            };
        }

        private static float2x2 clamp(float2x2 v, float2x2 mn, float2x2 mx)
        {
            return new float2x2(math.clamp(v.c0, mn.c0, mx.c0), math.clamp(v.c1, mn.c1, mx.c1));
        }

        private static float3x3 clamp(float3x3 v, float3x3 mn, float3x3 mx)
        {
            return new float3x3(
                math.clamp(v.c0, mn.c0, mx.c0),
                math.clamp(v.c1, mn.c1, mx.c1),
                math.clamp(v.c2, mn.c2, mx.c2)
            );
        }

        private static float4x4 clamp(float4x4 v, float4x4 mn, float4x4 mx)
        {
            return new float4x4(
                math.clamp(v.c0, mn.c0, mx.c0),
                math.clamp(v.c1, mn.c1, mx.c1),
                math.clamp(v.c2, mn.c2, mx.c2),
                math.clamp(v.c3, mn.c3, mx.c3)
            );
        }
    }
}