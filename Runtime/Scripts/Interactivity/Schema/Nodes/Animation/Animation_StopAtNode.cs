namespace UnityGLTF.Interactivity.Schema
{
    public class Animation_StopAtNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "animation/stopAt";
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [FlowOutSocketDescription()]
        public const string IdFlowError = "err";

        [FlowOutSocketDescription()]
        public const string IdFlowDone = "done";
        
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdValueAnimationRef = "animation";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueStopTime = "stopTime";
    }
}
