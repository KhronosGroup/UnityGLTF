using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatToAxisAngle : BehaviourEngineNode
    {
        private static readonly float3 UP = new float3(0f, 1f, 0f);

        public MathQuatToAxisAngle(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out float4 a);

            return id switch
            {
                ConstStrings.AXIS => new Property<float3>(Axis(a)),
                ConstStrings.ANGLE => new Property<float>(Angle(a)),
                _ => throw new InvalidOperationException($"Requested output {id} is not part of the spec for this node."),
            };
        }

        private float3 Axis(float4 a)
        {
            if (Mathf.Approximately(math.abs(a.w), 1f))
                return UP;

            var d = math.sqrt(1 - a.w * a.w);
            a /= d;

            return a.xyz;
        }

        private static float Angle(float4 a)
        {
            if (Mathf.Approximately(math.abs(a.w), 1f))
                return 0f;

            return 2f * math.acos(a.w);
        }
    }
}