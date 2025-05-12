using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public static partial class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 CubicBezier(float t, float2 cp0, float2 cp1)
        {
            var omt = 1 - t;
            return 3f * t * omt * omt * cp0 + 3f * t * t * omt * cp1 + t * t * t * (new float2(1f,1f));
        }

        public static float4 nlerp(float4 q1, float4 q2, float t)
        {
            float dt = math.dot(q1, q2);
            if (dt < 0.0f)
            {
                q2 = -q2;
            }
            
            return math.normalize(math.lerp(q1, q2, t));
        }

        public static float4 Slerpfloat4(float4 q1, float4 q2, float t)
        {
            float dt = math.dot(q1, q2);
            if (dt < 0.0f)
            {
                dt = -dt;
                q2 = -q2;
            }

            if (dt < 0.9995f)
            {
                float angle = math.acos(dt);
                float s = math.rsqrt(1.0f - dt * dt);    // 1.0f / sin(angle)
                float w1 = math.sin(angle * (1.0f - t)) * s;
                float w2 = math.sin(angle * t) * s;
                return q1 * w1 + q2 * w2;
            }
            else
            {
                // if the angle is small, use linear interpolation
                return nlerp(q1, q2, t);
            }
        }

        public static float2x2 LerpComponentwise(float2x2 from, float2x2 to, float t)
        {
            var c0 = math.lerp(from.c0, to.c0, t);
            var c1 = math.lerp(from.c1, to.c1, t);

            var m = new float2x2(c0, c1);

            return m;
        }

        public static float3x3 LerpComponentwise(float3x3 from, float3x3 to, float t)
        {
            var c0 = math.lerp(from.c0, to.c0, t);
            var c1 = math.lerp(from.c1, to.c1, t);
            var c2 = math.lerp(from.c2, to.c2, t);

            var m = new float3x3(c0, c1, c2);

            return m;
        }

        public static float4x4 LerpComponentwise(float4x4 from, float4x4 to, float t)
        {
            var c0 = math.lerp(from.c0, to.c0, t);
            var c1 = math.lerp(from.c1, to.c1, t);
            var c2 = math.lerp(from.c2, to.c2, t);
            var c3 = math.lerp(from.c3, to.c3, t);

            var m = new float4x4(c0, c1, c2, c3);

            return m;
        }
    }
}