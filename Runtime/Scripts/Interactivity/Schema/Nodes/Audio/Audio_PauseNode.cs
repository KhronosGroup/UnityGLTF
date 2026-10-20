using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// Pauses an audio source, keeping its playback position.
    /// The KHR_audio_emitter draft has no interactivity nodes yet, so the op is declared with the extension
    /// and viewers without support for it treat it as a no-op.
    /// </summary>
    public class Audio_PauseNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/pause";
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
