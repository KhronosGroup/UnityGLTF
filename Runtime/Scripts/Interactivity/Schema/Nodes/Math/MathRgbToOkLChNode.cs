namespace UnityGLTF.Interactivity.Schema
{
    public class Math_RgbToOkLChNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rgbToOkLCh";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputR = "r";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputG = "g";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputB = "b";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputL = "l";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputC = "c";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputH = "h";
        
    }
}