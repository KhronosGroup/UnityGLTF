using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnTickSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.OUT, "The flow to trigger after sending this event."),
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.TIME_SINCE_START, "Relative time in seconds since the graph execution start.", new Type[]  { typeof(float) }),
                new NodeValue(ConstStrings.TIME_SINCE_LAST_TICK, "Relative time in seconds since the last tick occurred.", new Type[]  { typeof(float) }),
            };

            return (flows, values);
        }
    }
}