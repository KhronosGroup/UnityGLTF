namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_SetDelayNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/setDelay";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdDuration = "duration";
        [FlowOutSocketDescription]
        public const string IdFlowOutError = "err";
        [FlowOutSocketDescription]
        public const string IdFlowDone = "done";
        [FlowInSocketDescription]
        public const string IdFlowInCancel = "cancel";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOutLastDelayIndex = "lastDelayIndex";
    }
}