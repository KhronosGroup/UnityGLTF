using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathQuatFromAxisAngle : BehaviourEngineNode
    {
        public MathQuatFromAxisAngle(BehaviourEngine engine, Node node) : base(engine, node)
        {
        }

        public override Variant GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.AXIS, out Variant axis);
            TryEvaluateValue(ConstStrings.ANGLE, out Variant angle);

            return axis.type switch
            {
                VariantType.Float3 when angle.type == VariantType.Float => Variant.FromFloat4(AxisAngle(axis.Float3, angle.Float)),
                _ => throw new InvalidOperationException($"Axis is a {axis.GetTypeSignature()}, expected float3. Angle is a {angle.GetTypeSignature()}, expected float."),
            };
        }

        private static float4 AxisAngle(float3 axis, float angle)
        {
            var sin = math.sin(0.5f * angle);
            var x = axis.x * sin;
            var y = axis.y * sin;
            var z = axis.z * sin;
            var w = math.cos(0.5f * angle);

            return new float4(x, y, z, w);
        }
    }
}