namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_DoNNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/doN";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowReset = "reset";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdN = "n";
        [FlowOutSocketDescription]
        public const string IdOut = "out";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdCurrentExecutionCount = "currentCount";
    }
}