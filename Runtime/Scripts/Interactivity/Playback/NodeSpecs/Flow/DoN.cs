
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowDoNSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow."),
                new NodeFlow(ConstStrings.RESET, "Resets the counter.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.N, "Number of times to repeat the out flow.", new System.Type[] { typeof(int) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "Activates immediately after the in flow.")
            };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.CURRENT_COUNT, "Current iteration index.", new System.Type[] { typeof(int) }),
        };

            return (flows, values);
        }
    }
}