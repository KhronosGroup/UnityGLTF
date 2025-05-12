using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public struct BezierInterpolateData
    {
        public IPointer pointer;
        public float duration;
        public float2 cp0;
        public float2 cp1;
        public NodeEngineCancelToken cancellationToken;
    }

    public static partial class Helpers
    {
        public static async Task<bool> InterpolateAsync<T,V>(T from, T to, Action<T> setter, Func<T, T, float, T> evaluator, float duration, V cancellationToken) where V : struct, ICancelToken
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (cancellationToken.isCancelled)
                    return false;

                setter(evaluator(from, to, t));
                await Task.Yield();
            }

            return true;
        }

        public static async Task<bool> LinearInterpolateAsync<T,V>(T to, Pointer<T> pointer, float duration, V cancellationToken) where V : struct, ICancelToken
        {
            return await InterpolateAsync(pointer.getter(), to, pointer.setter, pointer.evaluator, duration, cancellationToken);
        }

        public static async Task<bool> InterpolateBezierAsync<T>(Property<T> to, BezierInterpolateData d)
        {
            var v = to.value;
            return await InterpolateBezierAsync(v, d);
        }

        public static async Task<bool> InterpolateBezierAsync<T>(T to, BezierInterpolateData d)
        {
            var p = (Pointer<T>)d.pointer;
            
            var evaluator = new Func<T, T, float, T>((a, b, t) => p.evaluator(a, b, CubicBezier(t, d.cp0, d.cp1).y));

            return await InterpolateAsync(p.getter(), to, p.setter, evaluator, d.duration, d.cancellationToken);
        }

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