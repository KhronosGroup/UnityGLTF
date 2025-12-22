namespace UnityGLTF.Interactivity.Schema
{
    public class Variable_SetNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "variable/set";

        [ConfigDescription]
        public const string IdConfigVarIndices = "variables";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
    }
}