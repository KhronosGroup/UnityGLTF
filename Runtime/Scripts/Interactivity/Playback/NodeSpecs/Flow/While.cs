
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowWhileSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.CONDITION, "Loop condition.", new System.Type[] { typeof(bool) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.LOOP_BODY, "The flow to repeat index number times."),
                new NodeFlow(ConstStrings.COMPLETED, "The flow to execute once the end index has been reached.")
            };

            return (flows, null);
        }
    }
}