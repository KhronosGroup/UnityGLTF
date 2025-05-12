namespace UnityGLTF.Interactivity.Playback
{
    public class EventReceiveSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.EVENT, "Event id to receive.", typeof(int)),
            };
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