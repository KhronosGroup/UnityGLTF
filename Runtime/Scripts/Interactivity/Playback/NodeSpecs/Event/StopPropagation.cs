using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventStopPropagationSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.STOP_IMMEDIATE, "Also stop handlers of the same event that have not run yet.", new Type[] { typeof(bool) }),
                new NodeValue(ConstStrings.EVENT, "Event reference.", new Type[] { typeof(Ref) }),
            };

            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.OUT, "The flow to be activated after executing this operation.")
            };

            return (flows, null);
        }
    }
}
