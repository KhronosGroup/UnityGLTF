using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatMul : BehaviourEngineNode
    {
        public MathQuatMul(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float4> aProp when  b is Property<float4> bProp => new Property<float4>(Multiply(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 Multiply(float4 a, float4 b)
        {
            var x = a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y;
            var y = a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z;
            var z = a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x;
            var w = a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z;

            return new float4(x, y, z, w);
        }
    }
}