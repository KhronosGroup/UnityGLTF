using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>math/slerp for float2 and float3 vectors, interpolating direction and length.</summary>
    public class MathSlerp : BehaviourEngineNode
    {
        private const float EPSILON = 1e-6f;

        public MathSlerp(BehaviourEngine engine, Node node) : base(engine, node) { }

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.A, out IProperty a);
            TryEvaluateValue(ConstStrings.B, out IProperty b);
            TryEvaluateValue(ConstStrings.C, out float c);

            return a switch
            {
                Property<float2> pa when b is Property<float2> pb => new Property<float2>(Slerp(pa.value, pb.value, c)),
                Property<float3> pa when b is Property<float3> pb => new Property<float3>(Slerp(pa.value, pb.value, c)),
                _ => throw new InvalidOperationException("math/slerp requires a and b of the same float2 or float3 type."),
            };
        }

        public static float2 Slerp(float2 a, float2 b, float c)
        {
            var la = math.length(a);
            var lb = math.length(b);

            if (la < EPSILON || lb < EPSILON)
                return (1 - c) * a + c * b;

            var na = a / la;
            var nb = b / lb;
            var theta = math.acos(math.clamp(math.dot(na, nb), -1f, 1f));

            if (na.x * nb.y - na.y * nb.x < 0)
                theta = -theta;

            var l = (1 - c) * la + c * lb;
            math.sincos(c * theta, out var s, out var co);

            return new float2(na.x * co - na.y * s, na.x * s + na.y * co) * l;
        }

        public static float3 Slerp(float3 a, float3 b, float c)
        {
            var la = math.length(a);
            var lb = math.length(b);

            if (la < EPSILON || lb < EPSILON)
                return (1 - c) * a + c * b;

            var na = a / la;
            var nb = b / lb;
            var d = math.clamp(math.dot(na, nb), -1f, 1f);

            if (d > 1f - EPSILON)
                return (1 - c) * a + c * b;

            float3 r;

            if (d < -1f + EPSILON)
            {
                // Any unit vector perpendicular to a.
                var helper = math.abs(na.x) < 0.9f ? new float3(1, 0, 0) : new float3(0, 1, 0);
                r = math.normalize(math.cross(na, helper));
            }
            else
            {
                r = math.normalize(math.cross(na, nb));
            }

            var q = quaternion.AxisAngle(r, c * math.acos(d));
            var l = (1 - c) * la + c * lb;

            return math.mul(q, na) * l;
        }
    }
}
