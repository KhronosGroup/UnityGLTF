namespace UnityGLTF.Interactivity.Schema
{
    public class Animation_StopNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "animation/stop";
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [FlowOutSocketDescription()]
        public const string IdFlowError = "err";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueAnimation = "animation";
    }
}
