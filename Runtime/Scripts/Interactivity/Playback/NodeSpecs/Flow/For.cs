
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowForSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]{
                new NodeConfiguration(ConstStrings.INITIAL_INDEX, "Initial index.", typeof(int))
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.START_INDEX, "Index to start at.", new System.Type[] { typeof(int) }),
                new NodeValue(ConstStrings.END_INDEX, "Index to end at.", new System.Type[] { typeof(int) })
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.LOOP_BODY, "The flow to repeat index number times."),
                new NodeFlow(ConstStrings.COMPLETED, "The flow to execute once the end index has been reached.")
            };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.INDEX, "The current index", new System.Type[] { typeof(int) })
            };
            return (flows, values);
        }
    }
}