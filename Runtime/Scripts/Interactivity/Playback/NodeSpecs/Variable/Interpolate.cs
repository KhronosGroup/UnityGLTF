using System;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class VariableInterpolateSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.VARIABLE, "Variable to set.", typeof(int)),
                new NodeConfiguration(ConstStrings.USE_SLERP, "Whether to use spherical interpolation for quaternions.", typeof(bool)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.VALUE, "Value to set.", new Type[]  { typeof(int), typeof(float), typeof(float2), typeof(float3), typeof(float4), typeof(float2x2), typeof(float3x3), typeof(float4x4)}),
                new NodeValue(ConstStrings.DURATION, "The time, in seconds, in which the variable SHOULD reach the target value.", new Type[]  { typeof(float) }),
                new NodeValue(ConstStrings.P1, "Control point P1", new Type[]  { typeof(float2)}),
                new NodeValue(ConstStrings.P2, "Control point P2.", new Type[]  { typeof(float2)}),

            };

            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "Activates immediately after the in flow."),
                new NodeFlow(ConstStrings.ERR, "Activates if the duration value is invalid."),
                new NodeFlow(ConstStrings.DONE, "Activates after the delay is complete.")
            };

            return (flows, null);
        }
    }
}