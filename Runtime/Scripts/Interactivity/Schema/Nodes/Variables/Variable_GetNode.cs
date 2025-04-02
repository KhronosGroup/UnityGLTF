namespace UnityGLTF.Interactivity.Schema
{
    public class Variable_GetNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "variable/get";

        [ConfigDescription]
        public const string IdConfigVarIndex = "variable";
        
        [OutputSocketDescription]
        public const string IdOutputValue = "value";
    }
}