using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.Schema
{
    public class Debug_LogNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "debug/log";
        
        [ConfigDescription("")]
        public const string IdConfigMessage = "message";
        [ConfigDescription(0)]
        public const string IdConfigSeverity = "severity";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
    }
    
}