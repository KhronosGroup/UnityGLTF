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
                new NodeValue(ConstStrings.A, "Vector", new Type[]  { typeof(float3)}),
                new NodeValue(ConstStrings.B, "Axis", new Type[]  { typeof(float3)}),
                new NodeValue(ConstStrings.C, "Angle", new Type[]  { typeof(float)}),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Value", new Type[]  { typeof(float3) }),
            };

            return (null, values);
        }
    }
}