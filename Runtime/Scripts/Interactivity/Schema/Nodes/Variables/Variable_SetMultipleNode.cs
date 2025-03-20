namespace UnityGLTF.Interactivity.Schema
{
    public class Variable_SetMultipleNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "variable/setMultiple";

        [ConfigDescription]
        public const string IdConfigVarIndices = "variables";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        
    }
}