namespace UnityGLTF.Interactivity.Schema
{
    public class Math_RgbFromOkLChNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "math/rgbFromOkLCh";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputL = "l";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputC = "c";
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdInputH = "h";
        
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputR = "r";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputG = "g";
        [OutputSocketDescription(GltfTypes.Float)]
        public const string IdOutputB = "b";
    }
}