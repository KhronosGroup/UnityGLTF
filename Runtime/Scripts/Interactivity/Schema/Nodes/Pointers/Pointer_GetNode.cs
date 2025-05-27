namespace UnityGLTF.Interactivity.Schema
{
    public class Pointer_GetNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "pointer/get";

        [OutputSocketDescription()]
        public const string IdValue = "value";
        
        [OutputSocketDescription(GltfTypes.Bool)]
        public const string IdIsValid = "isValid";
        
        [ConfigDescription]
        public const string IdPointer = "pointer";
        [ConfigDescription]
        public const string IdPointerValueType = "type";
    }
}