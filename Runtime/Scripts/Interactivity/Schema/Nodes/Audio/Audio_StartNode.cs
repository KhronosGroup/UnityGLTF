namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// The world/startAnimation node plays a global animation
    ///
    /// See https://github.com/petermart/glTF-InteractivityGraph-AuthoringTool/blob/483da2161aa3d9c01ef47be8cdb2bec2d1dd3a18/src/authoring/AuthoringNodeSpecs.ts#L181 .
    /// </summary>
    ///

    public class Audio_StartNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/start";
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueAudio = "audioSourceIndex";

        [InputSocketDescription(GltfTypes.String)]
        public const string IdValueNode = "audioSourcePath";
    }
}
