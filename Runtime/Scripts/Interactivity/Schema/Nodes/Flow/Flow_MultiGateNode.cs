namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_MultiGateNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/multiGate";

        [ConfigDescription]
        public const string IdConfigIsRandom = "isRandom";
        [ConfigDescription]
        public const string IdConfigIsLoop = "isLoop";
        
        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowInSocketDescription]
        public const string IdFlowInReset = "reset";
        
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdLastIndex = "lastIndex";
       
    }
}