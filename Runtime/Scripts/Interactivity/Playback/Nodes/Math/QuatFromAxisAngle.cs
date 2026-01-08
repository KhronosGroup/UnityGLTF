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

        public override IProperty GetOutputValue(string id)
        {
            TryEvaluateValue(ConstStrings.AXIS, out IProperty axis);
            TryEvaluateValue(ConstStrings.ANGLE, out IProperty angle);

            return axis switch
            {
                Property<float3> axisProp when angle is Property<float> angleProp => new Property<float4>(AxisAngle(axisProp.value, angleProp.value)),
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