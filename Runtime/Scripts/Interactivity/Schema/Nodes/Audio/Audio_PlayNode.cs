using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    /// <summary>
    /// Starts playing an audio source from the beginning. "out" is triggered right after the playback started.
    /// The KHR_audio_emitter draft has no interactivity nodes yet, so the op is declared with the extension
    /// and viewers without support for it treat it as a no-op.
    /// </summary>
    public class Audio_PlayNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "audio/play";
        public override string Extension { get; protected set; } = KHR_audio_emitter.ExtensionName;
        
        [FlowInSocketDescription()]
        public const string IdFlowIn = "in";
        
        [FlowOutSocketDescription()]
        public const string IdFlowOut = "out";
        
        [FlowOutSocketDescription()]
        public const string IdFlowError = "err";
        
        /// <summary> Triggered when the source finished playing (not for looping sources). </summary>
        [FlowOutSocketDescription()]
        public const string IdFlowDone = "done";
        
        [InputSocketDescription(GltfTypes.Ref)]
        public const string IdValueSourceRef = "source";
    }
}
