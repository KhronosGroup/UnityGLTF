using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRotate2DSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Vector to Rotate", new Type[]  { typeof(float2)}),
                new NodeValue(ConstStrings.ANGLE, "Angle in Radians", new Type[]  { typeof(float)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Rotated Vector", new Type[]  { typeof(float2) }),
            };

            return (null, values);
        }
    }
}