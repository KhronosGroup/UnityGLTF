namespace UnityGLTF.Interactivity.Schema
{
    public class Flow_WhileNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "flow/while";

        [FlowInSocketDescription]
        public const string IdFlowIn = "in";
        [FlowOutSocketDescription]
        public const string IdLoopBody = "loopBody";
        [FlowOutSocketDescription]
        public const string IdCompleted = "completed";
        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdCondition = "condition";
    }
}