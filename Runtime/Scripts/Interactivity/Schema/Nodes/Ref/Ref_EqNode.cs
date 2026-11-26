namespace UnityGLTF.Interactivity.Schema
{
    public class Ref_EqNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "ref/eq";

        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdValueA = "a";
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdValueB = "b";     
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOutValue = "value";
        
            
    }
}