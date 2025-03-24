namespace UnityGLTF.Interactivity.Schema
{
    public class Type_BoolToIntNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/boolToInt";

        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdValueResult = "value";
    }
}