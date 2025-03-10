namespace UnityGLTF.Interactivity.Schema
{
    public class Event_SendNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/send";

        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [ConfigDescription]
        public const string IdEvent = "event";
    }
}