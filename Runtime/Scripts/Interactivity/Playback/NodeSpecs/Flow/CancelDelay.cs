
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowCancelDelaySpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.DELAY_INDEX, "Index of the delay to cancel.", new System.Type[] { typeof(int) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.OUT, "Activates immediately after the in flow.")
            };

            return (flows, null);
        }
    }
}