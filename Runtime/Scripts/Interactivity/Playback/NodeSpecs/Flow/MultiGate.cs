
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowMultiGateSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]
            {
                new NodeConfiguration(ConstStrings.IS_RANDOM, "If set to true, output flows are activated in random order, picking a random not used output flow each time until all are done; false in the default configuration.", typeof(bool)),
                new NodeConfiguration(ConstStrings.IS_LOOP, "If set to true, output flow activations will repeat in a loop continuously after all are done; false in the default configuration.", typeof(bool)),
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow."),
                new NodeFlow(ConstStrings.RESET, "When this flow is activated, the lastIndex value is reset to -1 and all outputs are marked as not used."),
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.DURATION, "The time, in seconds, to wait after an output flow activation before allowing subsequent output flow activations.", new System.Type[] { typeof(float) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var values = new NodeValue[]{
                new NodeValue(ConstStrings.LAST_INDEX, "The index of the last used output; -1 if the node has not been activated.", new System.Type[] { typeof(int) }),
        };

            return (null, values);
        }
    }
}