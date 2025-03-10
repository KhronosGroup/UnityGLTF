namespace UnityGLTF.Interactivity.Schema
{
    public class Event_OnStartNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/onStart";

        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
    }
}