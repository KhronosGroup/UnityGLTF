namespace UnityGLTF.Interactivity.Schema
{
    public class Type_FloatToIntNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/floatToInt";

        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdValueResult = "value";
    }
}