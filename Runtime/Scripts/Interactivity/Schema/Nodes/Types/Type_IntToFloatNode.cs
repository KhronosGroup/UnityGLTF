namespace UnityGLTF.Interactivity.Schema
{
    public class Type_IntToFloatNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/intToFloat";

        [InputSocketDescription(GltfTypes.Int)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdValueResult = "value";
    }
}