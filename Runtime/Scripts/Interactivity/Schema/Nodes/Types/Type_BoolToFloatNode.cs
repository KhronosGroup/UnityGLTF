namespace UnityGLTF.Interactivity.Schema
{
    public class Type_BoolToFloatNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/boolToFloat";

        [InputSocketDescription(GltfTypes.Bool)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueResult = "value";
    }
}