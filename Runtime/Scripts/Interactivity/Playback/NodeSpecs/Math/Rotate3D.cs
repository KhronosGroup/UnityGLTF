using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathRotate3DSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.A, "Vector to Rotate", new Type[]  { typeof(float3)}),
                new NodeValue(ConstStrings.ROTATION, "Rotation Quaternion", new Type[]  { typeof(float4)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Rotated Vector", new Type[]  { typeof(float3) }),
            };

            return (null, values);
        }
    }
}