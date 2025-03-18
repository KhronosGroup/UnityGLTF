namespace UnityGLTF.Interactivity.Schema
{
    public class Type_IntToBoolNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "type/intToBool";

        [InputSocketDescription(GltfTypes.Int)]
        public const string IdInputA = "a";
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdValueResult = "value";
    }
}