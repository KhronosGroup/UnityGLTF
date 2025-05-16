namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_ForLoopNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/for";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription]
        public const string IdLoopBody = "loopBody";
        [FlowOutSocketDescription]
        public const string IdCompleted = "completed";

        [InputSocketDescription(GltfTypes.Int)]
        public const string IdStartIndex = "startIndex";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdEndIndex = "endIndex";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdIndex = "index";
        [ConfigDescription(0)]
        public const string IdConfigInitialIndex = "initialIndex";
    }
}