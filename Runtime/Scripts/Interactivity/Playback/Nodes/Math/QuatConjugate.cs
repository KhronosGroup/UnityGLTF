using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatConjugate : BehaviourEngineNode
    {
        public MathQuatConjugate(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);

            return a switch
            {
                Property<float4> aProp => new Property<float4>(Conjugate(aProp.value)),
                _ => throw new InvalidOperationException($"Input A is a {a.GetTypeSignature()} and not a float4!"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 Conjugate(float4 a)
        {
            return new float4(-a.x, -a.y, -a.z, a.w);
        }
    }
}