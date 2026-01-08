using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Extensions
{
    public static partial class ExtensionMethods
    {
        public static float3 ToFloat3(this Color c)
        {
            return new float3(c.r, c.g, c.b);
        }

        public static float3 ToFloat3(this Color3 c)
        {
            return new float3(c.r, c.g, c.b);
        }

        public static Color ToColor(this float3 v)
        {
            return new Color(v.x, v.y, v.z, 1f);
        }

        public static float4 ToFloat4(this Color c)
        {
            return new float4(c.r, c.g, c.b, c.a);
        }

        public static Color ToColor(this float4 v)
        {
            return new Color(v.x, v.y, v.z, v.w);
        }

        /// <summary>
        /// Converts from the Unity to GLTF coordinate system for a quaternion and returns it as a float4.
        /// </summary>
        /// <param name="q">Unity coordinate system quaternion.</param>
        /// <returns>GLTF coordinate system float4 representing quaternion.</returns>
        public static float4 ToGLTFFloat4(this quaternion q)
        {
            return q.value;
        }

        public static float4 ToFloat4(this quaternion q)
        {
            return q.value;
        }

        public static float4 ToFloat4(this Quaternion q)
        {
            return new float4(q.x, q.y, q.z, q.w);
        }

        public static quaternion ToQuaternion(this float4 v)
        {
            return new quaternion(v.x, v.y, v.z, v.w);
        }
    }
}