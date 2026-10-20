namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSequenceSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow.")
            };
            return (flows, null);
        }
    }
}