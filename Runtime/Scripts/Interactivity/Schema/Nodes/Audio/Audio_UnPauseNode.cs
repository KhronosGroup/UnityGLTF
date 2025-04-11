namespace UnityGLTF.Interactivity.Schema
{
     /// <summary>
	/// The audio unpause node schema / data
    /// </summary>
    public class Audio_UnPauseNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/unpause";
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueAudio = "audioSourceIndex";

        //[InputSocketDescription(GltfTypes.String)]
        //public const string IdValueNode = "audioSourcePath";
    }
}
