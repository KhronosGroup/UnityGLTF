namespace UnityGLTF.Interactivity.Schema
{
    public class Variable_InterpolateNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "variable/interpolate";

        [ConfigDescription]
        public const string IdConfigVariable = "variable";
        [ConfigDescription]
        public const string IdUseSlerp = "useSlerp";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [FlowOutSocketDescription]
        public const string IdFlowOutError = "err";
        [FlowOutSocketDescription]
        public const string IdFlowOutDone = "done";
        
        [InputSocketDescription()]
        public const string IdValue = "value";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdDuration = "duration";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdPoint1 = "p1";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdPoint2 = "p2";
    }
}