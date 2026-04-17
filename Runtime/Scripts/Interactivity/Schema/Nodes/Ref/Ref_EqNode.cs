namespace UnityGLTF.Interactivity.Schema
{
    public class Ref_EqNode: GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "ref/eq";

        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdA = "a";
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdB = "b";     
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdOutValue = "value";
        
            
    }
}