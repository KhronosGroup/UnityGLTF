
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWaitAllSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.INPUT_FLOWS, "The number of input flows; zero in the default configuration.", typeof(int)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.RESET, "When this flow is activated, all input flows are marked as unused."),
        };

            return (flows, null);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "The flow to be activated after every input flow activation except the last missing input."),
                new NodeFlow(ConstStrings.COMPLETED, "The flow to be activated when the last missing input flow is activated."),
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.REMAINING_INPUTS, "The number of not yet activated input flows.", new System.Type[] { typeof(int) }),
        };

            return (null, values);
        }
    }
}