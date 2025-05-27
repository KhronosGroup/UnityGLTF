namespace UnityGLTF.Interactivity.Schema
{
    public class Pointer_SetNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "pointer/set";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [FlowOutSocketDescription]
        public const string IdFlowOutError = "err";
        
        [InputSocketDescription]
        public const string IdValue = "value";
        
        [ConfigDescription]
        public const string IdPointer = "pointer";
        [ConfigDescription]
        public const string IdPointerValueType = "type";
    }
}