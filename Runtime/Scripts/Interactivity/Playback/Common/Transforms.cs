using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Extensions
{
    public static class TransformExtensions
    {
        public static void Decompose(
       this Matrix4x4 m,
       out Vector3 translation,
       out Quaternion rotation,
       out Vector3 scale
       )
        {
            translation = new Vector3(m.m03, m.m13, m.m23);
            var mRotScale = new float3x3(
                m.m00, m.m01, m.m02,
                m.m10, m.m11, m.m12,
                m.m20, m.m21, m.m22
                );
            mRotScale.Decompose(out float4 mRotation, out float3 mScale);
            rotation = new Quaternion(mRotation.x, mRotation.y, mRotation.z, mRotation.w);
            scale = new Vector3(mScale.x, mScale.y, mScale.z);
        }

        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(
            this float4x4 m,
            out float3 translation,
            out float4 rotation,
            out float3 scale
            )
        {
            var mRotScale = new float3x3(
                m.c0.xyz,
                m.c1.xyz,
                m.c2.xyz
                );
            mRotScale.Decompose(out rotation, out scale);
            translation = m.c3.xyz;
        }

        /// <summary>
        /// Decomposes a 3x3 matrix into rotation and scale
        /// </summary>
        /// <param name="rotation">Rotation quaternion values</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(this float3x3 m, out float4 rotation, out float3 scale)
        {
            var lenC0 = math.length(m.c0);
            var lenC1 = math.length(m.c1);
            var lenC2 = math.length(m.c2);

            float3x3 rotationMatrix;
            rotationMatrix.c0 = m.c0 / lenC0;
            rotationMatrix.c1 = m.c1 / lenC1;
            rotationMatrix.c2 = m.c2 / lenC2;

            scale.x = lenC0;
            scale.y = lenC1;
            scale.z = lenC2;

            if (rotationMatrix.IsNegative())
            {
                rotationMatrix *= -1f;
                scale *= -1f;
            }

            // Inlined normalize(rotationMatrix)
            rotationMatrix.c0 = math.normalize(rotationMatrix.c0);
            rotationMatrix.c1 = math.normalize(rotationMatrix.c1);
            rotationMatrix.c2 = math.normalize(rotationMatrix.c2);

            rotation = new quaternion(rotationMatrix).value;
        }

        static bool IsNegative(this float3x3 m)
        {
            var cross = math.cross(m.c0, m.c1);
            return math.dot(cross, m.c2) < 0f;
        }

        public static float4x4 GetWorldMatrix(this Transform t, bool worldSpace, bool rightHanded)
        {
            float3 pos;
            quaternion rot;
            float3 scale;

            if (worldSpace)
            {
                pos = t.position;
                rot = t.rotation;
                scale = t.lossyScale;
            }
            else
            {
                pos = t.localPosition;
                rot = t.localRotation;
                scale = t.localScale;
            }

            var m = float4x4.TRS(pos, rot, scale);

            if (rightHanded)
            {
                var c0 = new float4(-1f, 0f, 0f, 0f);
                var c1 = new float4(0f, 1f, 0f, 0f);
                var c2 = new float4(0f, 0f, 1f, 0f);
                var c3 = new float4(0f, 0f, 0f, 1f);
                var c = new float4x4(c0, c1, c2, c3);

                m = math.mul(math.mul(c, m), math.transpose(c));
            }

            return m;
        }

        public static void SetWorldMatrix(this Transform t, float4x4 m, bool worldSpace, bool rightHanded)
        {
            if (rightHanded)
            {
                var c0 = new float4(-1f, 0f, 0f, 0f);
                var c1 = new float4(0f, 1f, 0f, 0f);
                var c2 = new float4(0f, 0f, 1f, 0f);
                var c3 = new float4(0f, 0f, 0f, 1f);
                var c = new float4x4(c0, c1, c2, c3);

                m = math.mul(math.mul(c, m), math.transpose(c));
            }

            Decompose((Matrix4x4)m, out var pos, out var rot, out var scale);

            if (worldSpace)
            {
                t.SetPositionAndRotation(pos, rot);
                t.SetGlobalScale(scale);
            }
            else
            {
                t.SetLocalPositionAndRotation(pos, rot);
                t.localScale = scale;
            }

        }

        public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
        {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
        }

        public static float3 SwapHandedness(this float3 v)
        {
            return new float3(-v.x, v.y, v.z);
        }

        public static Vector3 SwapHandedness(this Vector3 v)
        {
            return new Vector3(-v.x, v.y, v.z);
        }

        public static Quaternion SwapHandedness(this Quaternion q)
        {
            // TODO: Figure out if there's a way to do this without converting to euler angles and back as it's really slow.
            var euler = q.eulerAngles;

            euler.y *= -1;
            euler.z *= -1;

            return Quaternion.Euler(euler);
        }
    }
}