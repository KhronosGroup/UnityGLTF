using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// Stops an audio source and rewinds it to the beginning.
    /// The KHR_audio_emitter draft has no interactivity nodes yet, so the op is declared with the extension
    /// and viewers without support for it treat it as a no-op.
    /// </summary>
    public class Audio_StopNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/stop";
        public override string Extension { get; protected set; } = KHR_audio_emitter.ExtensionName;
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [FlowOutSocketDescription()]
        public const string IdFlowError = "err";
        
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdValueSourceRef = "source";
    }
}
