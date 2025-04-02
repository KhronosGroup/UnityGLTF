namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_BranchNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/branch";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdCondition = "condition";
        [FlowOutSocketDescription]
        public const string IdFlowOutTrue = "true";
        [FlowOutSocketDescription]
        public const string IdFlowOutFalse = "false";
    }
}