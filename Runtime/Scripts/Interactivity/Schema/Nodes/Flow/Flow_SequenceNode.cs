namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_SequenceNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/sequence";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
    }
}