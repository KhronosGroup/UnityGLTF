namespace UnityGLTF.Interactivity.Schema
{
    public class Babylon_LogNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "babylon/log";
        public override string Extension { get; protected set; } = EXTENSION_ID; 

        public const string EXTENSION_ID = "babylon/log";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription]
        public const string IdMessage = "message";
    }
}