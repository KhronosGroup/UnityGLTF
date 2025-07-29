namespace UnityGLTF.Interactivity.Schema
{
    public class Pointer_InterpolateNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "pointer/interpolate";

        [ConfigDescription]
        public const string IdPointer = "pointer";
        [ConfigDescription]
        public const string IdPointerValueType = "type";

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
        [InputSocketDescription(GltfTypes.Float2)]
        public const string IdPoint1 = "p1";
        [InputSocketDescription(GltfTypes.Float2)]
        public const string IdPoint2 = "p2";
    }
}