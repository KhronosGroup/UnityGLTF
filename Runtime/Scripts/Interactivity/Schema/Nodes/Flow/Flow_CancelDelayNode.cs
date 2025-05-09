namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_CancelDelayNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/cancelDelay";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdDelayIndex = "delayIndex";
    }
}