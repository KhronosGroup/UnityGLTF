using Newtonsoft.Json.Linq;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public struct Color3
    {
        public float r;
        public float g;
        public float b;

        public Color3(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static implicit operator Color(Color3 c) => new Color(c.r, c.g, c.b, 1f);
        public static implicit operator Color3(Color c) => new Color3(c.r, c.g, c.b);
    }

    public static class Parser
    {
        public static float ToFloat(JArray jArray)
        {
            if (jArray == null)
                return float.NaN;

            return jArray[0].Value<float>();
        }

        public static int ToInt(JArray jArray)
        {
            if (jArray == null)
                return 0;

            return jArray[0].Value<int>();
        }

        public static bool ToBool(JArray jArray)
        {
            if (jArray == null)
                return false;

            return jArray[0].Value<bool>();
        }

        public static string ToString(JArray jArray)
        {
            if (jArray == null)
                return "";

            return jArray[0].Value<string>();
        }

        public static float2 ToFloat2(JArray jArray)
        {
            if (jArray == null)
                return new float2(float.NaN, float.NaN);

            return new float2(jArray[0].Value<float>(), jArray[1].Value<float>());
        }

        public static float3 ToFloat3(JArray jArray)
        {
            if (jArray == null)
                return new float3(float.NaN, float.NaN, float.NaN);

            return new float3(jArray[0].Value<float>(), jArray[1].Value<float>(), jArray[2].Value<float>());
        }

        public static float4 ToFloat4(JArray jArray)
        {
            if (jArray == null)
                return new float4(float.NaN, float.NaN, float.NaN, float.NaN);

            return new float4(jArray[0].Value<float>(), jArray[1].Value<float>(), jArray[2].Value<float>(), jArray[3].Value<float>());
        }

        public static int[] ToIntArray(JArray jArray)
        {
            if (jArray == null)
                return null;

            var arr = new int[jArray.Count];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = jArray[i].Value<int>();
            }

            return arr;
        }

        public static float2x2 ToFloat2x2(JArray jArray)
        {
            const int MATRIX_SIZE = 4;

            if (jArray == null)
            {
                var m = new float2x2();

                for (int i = 0; i < MATRIX_SIZE; i++)
                {
                    m[i] = float.NaN;
                }

                return m;
            }

            // GLTF floatNxN are column-major and Unity.Mathematics floatNxN are ROW-MAJOR so we need to be careful.
            var c0 = new Vector2(v(0), v(1));
            var c1 = new Vector2(v(2), v(3));

            return new float2x2(c0, c1);

            // Helper to reduce bloat.
            float v(int index)
            {
                return jArray[index].Value<float>();
            }
        }

        public static float3x3 ToFloat3x3(JArray jArray)
        {
            const int MATRIX_SIZE = 9;


            if (jArray == null)
            {
                var m = new float3x3();

                for (int i = 0; i < MATRIX_SIZE; i++)
                {
                    m[i] = float.NaN;
                }

                return m;
            }

            // GLTF floatNxN are column-major and Unity.Mathematics floatNxN are ROW-MAJOR so we need to be careful.
            var c0 = new float3(v(0), v(1), v(2));
            var c1 = new float3(v(3), v(4), v(5));
            var c2 = new float3(v(6), v(7), v(8));

            return new float3x3(c0, c1, c2);

            // Helper to reduce bloat.
            float v(int index)
            {
                return jArray[index].Value<float>();
            }
        }

        public static float4x4 ToFloat4x4(JArray jArray)
        {
            const int MATRIX_SIZE = 16;

            if (jArray == null)
            {
                var m = new float4x4();

                for (int i = 0; i < MATRIX_SIZE; i++)
                {
                    m[i] = float.NaN;
                }

                return m;
            }

            // Unity Matrix4x4 and GLTF both use Column-Major matrices so we can do a 1:1 transfer.
            // Unity.Mathematics.float4x4 is ROW-MAJOR so we need to be careful.
            var c0 = new float4(v(0), v(1), v(2), v(3));
            var c1 = new float4(v(4), v(5), v(6), v(7));
            var c2 = new float4(v(8), v(9), v(10), v(11));
            var c3 = new float4(v(12), v(13), v(14), v(15));

            return new float4x4(c0, c1, c2, c3);

            // Helper to reduce bloat.
            float v(int index)
            {
                return jArray[index].Value<float>();
            }
        }
    }
}