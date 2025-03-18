namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_WaitAllNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/waitAll";
        
        [FlowOutSocketDescription]
        public const string IdFlowOutCompleted = "completed";
        [FlowOutSocketDescription]
        public const string IdFlowOutNotCompleted = "out";
        [FlowInSocketDescription]
        public const string IdFlowInReset = "reset";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOutRemainingInputs = "remainingInputs";
        [ConfigDescription]
        public const string IdConfigInputFlows = "inputFlows";
    }
}