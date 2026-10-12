
namespace UnityGLTF.Interactivity.Playback
{
    public class FlowSwitchSpec : NodeSpecifications
    {
        protected override NodeConfiguration[] GenerateConfiguration()
        {
            return new NodeConfiguration[]{
                new NodeConfiguration(ConstStrings.CASES, "The cases on which to perform the switch; empty in the default configuration", typeof(int[]))
            };
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.IN, "The in flow."),
        };

            var values = new NodeValue[]{
                new NodeValue(ConstStrings.SELECTION, "Which output flow to trigger.", new System.Type[] { typeof(int) }),
        };
            return (flows, values);
        }

        protected override (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            var flows = new NodeFlow[]{
                new NodeFlow(ConstStrings.DEFAULT, "The output flow activated when the selection input value is not present in the cases configuration array"),
            };            
            
            return (flows, null);
        }
    }
}