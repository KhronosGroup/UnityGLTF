namespace UnityGLTF.Interactivity.Schema
{
    public class Type_BoolToIntNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/boolToInt";

        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdInputA = "a";
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueResult = "value";
    }
}