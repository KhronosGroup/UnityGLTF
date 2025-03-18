namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// The world/startAnimation node plays a global animation
    ///
    /// See https://github.com/petermart/glTF-InteractivityGraph-AuthoringTool/blob/483da2161aa3d9c01ef47be8cdb2bec2d1dd3a18/src/authoring/AuthoringNodeSpecs.ts#L181 .
    /// </summary>
    ///
    
    public class Animation_StartNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "animation/start";
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [FlowOutSocketDescription()]
        public const string IdFlowDone = "done";
        
        [InputSocketDescription(GltfTypes.Int)]
        public const string IdValueAnimation = "animation";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueSpeed = "speed";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueStartTime = "startTime";
        
        [InputSocketDescription(GltfTypes.Float)]
        public const string IdValueEndtime = "endTime";
    }
}
