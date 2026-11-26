using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// Continues a paused audio source from its playback position.
    /// The KHR_audio_emitter draft has no interactivity nodes yet, so the op is declared with the extension
    /// and viewers without support for it treat it as a no-op.
    /// </summary>
    public class Audio_UnpauseNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/unpause";
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
