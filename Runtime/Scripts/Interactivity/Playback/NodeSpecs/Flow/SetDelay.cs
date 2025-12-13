
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSetDelaySpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow."),
                new NodeFlow(ConstStrings.CANCEL, "Cancels all delays from this node.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.DURATION, "Duration of this delay.", new System.Type[] { typeof(float) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "Activates immediately after the in flow."),
                new NodeFlow(ConstStrings.ERR, "Activates if the duration value is invalid."),
                new NodeFlow(ConstStrings.DONE, "Activates after the delay is complete.")
            };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.LAST_DELAY_INDEX, "Last unique delay index from this node", new System.Type[] { typeof(int) })
            };
            return (flows, values);
        }
    }
}