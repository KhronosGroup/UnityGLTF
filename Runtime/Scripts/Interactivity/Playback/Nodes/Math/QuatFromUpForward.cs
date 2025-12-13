using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityGLTF.Interactivity.Playback.Extensions;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatFromUpForward : BehaviourEngineNode
    {
        public MathQuatFromUpForward(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.UP, out float3 up);
            TryEvaluateValue(ConstStrings.FORWARD, out float3 forward);

            return new Property<float4>(QuaternionFromUpAndForwardDirection(up, forward));
        }

        private static float4 QuaternionFromUpAndForwardDirection(float3 up, float3 forward)
        {
            var c = math.abs(math.dot(up, forward));
            float3 s;

            if (Mathf.Approximately(c, 1f))
                s = GeneratePerpendicularUnitVector(forward);
            else
                s = math.normalize(math.cross(up, forward));

            var t = math.cross(forward, s);

            var m = new float3x3(s, t, forward);

            return math.quaternion(m).ToFloat4();
        }

        private static float3 GeneratePerpendicularUnitVector(float3 a)
        {
            var x = CopySign(a.z, a.x);
            var y = CopySign(a.z, a.y);
            var z = -CopySign(a.x, a.z) - CopySign(a.y, a.z);

            return new float3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CopySign(float a, float b)
        {
            return (b >= 0 ? 1f : -1f) * math.abs(a);
        }
    }
}