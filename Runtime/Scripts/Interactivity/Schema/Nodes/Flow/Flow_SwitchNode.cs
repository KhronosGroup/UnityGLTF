namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_SwitchNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/switch";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdSelection = "selection";
        [FlowOutSocketDescription]
        public const string IdFDefaultFlowOut = "default";
        
        
    }
}