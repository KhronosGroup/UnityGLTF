namespace UnityGLTF.Interactivity.Schema
{
    public class Event_ReceiveNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/receive";

        [ConfigDescription()]
        public const string IdEventConfig = "event";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
    }
}