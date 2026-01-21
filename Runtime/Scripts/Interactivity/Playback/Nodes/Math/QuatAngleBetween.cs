using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatAngleBetween : BehaviourEngineNode
    {
        public MathQuatAngleBetween(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);

            return a switch
            {
                Property<float4> aProp when b is Property<float4> bProp => new Property<float>(AngleBetween(aProp.value, bProp.value)),
                _ => throw new InvalidOperationException($"No supported type found for input A: {a.GetTypeSignature()} or input type did not match B: {b.GetTypeSignature()}."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float AngleBetween(float4 a, float4 b)
        {
            return 2f * math.acos(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w);
        }
    }
}