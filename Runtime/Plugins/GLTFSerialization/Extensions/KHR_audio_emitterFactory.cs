using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
    public class KHR_audio_emitterFactory : ExtensionFactory
    {	
        public const string EXTENSION_NAME = KHR_audio_emitter.ExtensionName;

        public KHR_audio_emitterFactory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            // Positional audio emitter
            JToken ermitterToken = extensionToken.Value[nameof(KHR_NodeAudioEmitterRef.emitter)];
            if (ermitterToken != null)
            {
                return KHR_NodeAudioEmitterRef.Deserialize(root, extensionToken);
            }

            var audioToken = extensionToken.Value[nameof(KHR_audio_emitter.audio)];
            var sourcesToken = extensionToken.Value[nameof(KHR_audio_emitter.sources)];

            if (audioToken == null && sourcesToken == null)
            {
                // Global audio emitter
                JToken globalToken = extensionToken.Value[nameof(KHR_SceneAudioEmittersRef.emitters)];
                if (globalToken != null)
                {
                    return KHR_SceneAudioEmittersRef.Deserialize(root, extensionToken);
                }
            }
            
            var extension = new KHR_audio_emitter();
            
            if (audioToken != null)
            {
                JArray audioArray = audioToken as JArray;
                foreach (var audio in audioArray.Children())
                    extension.audio.Add(KHR_AudioData.Deserialize(root, audio.CreateReader()));
            }
            
            if (sourcesToken != null)
            {
                JArray sourcesArray = sourcesToken as JArray;
                foreach (var source in sourcesArray.Children())
                    extension.sources.Add(KHR_AudioSource.Deserialize(root, source.CreateReader()));
            }
            
            var emittersToken = extensionToken.Value[nameof(KHR_audio_emitter.emitters)];
            if (emittersToken != null)
            {
                JArray emittersArray = emittersToken as JArray;
                foreach (var emitters in emittersArray.Children())
                    extension.emitters.Add(KHR_AudioEmitter.Deserialize(root, emitters.CreateReader()));
            }

            return extension;
        }
    }
    
}