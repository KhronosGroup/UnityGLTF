
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowThrottleSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow."),
                new NodeFlow(ConstStrings.RESET, "When this flow is activated, the output flow throttling state is reset."),
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.DURATION, "The time, in seconds, to wait after an output flow activation before allowing subsequent output flow activations.", new System.Type[] { typeof(float) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "The flow to be activated if the output flow is not currently throttled"),
                new NodeFlow(ConstStrings.ERR, "The flow to be activated if the duration input value is negative, infinite, or NaN"),

            };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.LAST_REMAINING_TIME, "The remaining throttling time, in seconds, at the moment of the last valid activation of the input flow or NaN if the input flow has never been activated with a valid duration input value.", new System.Type[] { typeof(float) }),
        };

            return (flows, values);
        }
    }
}