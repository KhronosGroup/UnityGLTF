using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathMatComposeSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.TRANSLATION, "Translation", new Type[]  { typeof(float3) }),
                new NodeValue(ConstStrings.ROTATION, "Rotation", new Type[]  { typeof(float4) }),
                new NodeValue(ConstStrings.SCALE, "Scale", new Type[]  { typeof(float3) }),
            };

            return (null, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Matrix", new Type[]  { typeof(float4x4) }),
            };

            return (null, values);
        }
    }
}