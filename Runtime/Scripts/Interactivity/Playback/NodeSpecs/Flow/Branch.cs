
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowBranchSpec : NodeSpecifications
    {
        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow.")
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.CONDITION, "Condition to evaluate.", new System.Type[] { typeof(bool) })
            };
            return (flows, values);
        }
    }
}