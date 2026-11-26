namespace UnityGLTF.Interactivity.Schema
{
    public class Event_StopPropagationNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/stopPropagation";

        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdStopImmediate = "stopImmediate";
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdEvent = "event";
    }
}