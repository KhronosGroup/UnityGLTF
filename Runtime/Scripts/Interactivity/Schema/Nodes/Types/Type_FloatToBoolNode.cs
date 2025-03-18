namespace UnityGLTF.Interactivity.Schema
{
    public class Type_FloatToBoolNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/floatToBool";

        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdValueResult = "value";
    }
}