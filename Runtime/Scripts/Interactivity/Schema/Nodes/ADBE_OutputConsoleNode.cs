namespace UnityGLTF.Interactivity.Schema
{
    public class ADBE_OutputConsoleNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "ADBE/output_console_node";
        public override string Extension { get; protected set; } = EXTENSION_ID;
        
        public const string EXTENSION_ID = "ADBE_output_console_node";
       
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription]
        public const string IdMessage = "message";
    }
}