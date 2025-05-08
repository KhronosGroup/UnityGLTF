namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_ThrottleNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/throttle";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowInSocketDescription]
        public const string IdFlowReset = "reset";
        
        [InputSocketDescription]
        public const string IdInputDuration = "duration";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [FlowOutSocketDescription]
        public const string IdFlowOutError = "err";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutElapsedTime = "lastRemainingTime";
    }
}