using System;

namespace UnityGLTF.Interactivity.Playback
{
    public class EventOnStartSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.OUT, "The flow to trigger when the session starts.")
            };

            var values = new NodeValue[]
            {
                new NodeValue(ConstStrings.EVENT, "The event reference.", new Type[] { typeof(Ref) }),
            };

            return (flows, values);
        }
    }
}
