namespace UnityGLTF.Interactivity.Playback
{
    public class EventSendSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.EVENT, "Event id to send.", typeof(int)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };

            return (flows, null);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]
            {
                new NodeFlow(ConstStrings.OUT, "The flow to trigger after sending this event."),
            };

            return (flows, null);
        }
    }
}