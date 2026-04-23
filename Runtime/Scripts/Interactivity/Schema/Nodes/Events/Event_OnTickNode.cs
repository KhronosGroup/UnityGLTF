namespace UnityGLTF.Interactivity.Schema
{
    public class Event_OnTickNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/onTick";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutTimeSinceStart = "timeSinceStart";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutTimeSinceLastTick = "timeSinceLastTick";
        
        [OutputSocketDescription(GltfTypes.Ref)]
        public const string IdEvent = "event";
    }
}