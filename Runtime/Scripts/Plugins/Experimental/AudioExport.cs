using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTF.Schema;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF.Plugins
{
    [NonRatifiedPlugin]
    public class AudioExport: GLTFExportPlugin
    {
#if !UNITY_EDITOR
        public override bool Enabled => false;
#endif
        public override bool EnabledByDefault => false;
        public override string DisplayName => "KHR_audio_emitter";
        public override string Description => "Exports positional and global audio sources. Only exporting from the editor is supported at this time.";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
#if UNITY_EDITOR
            return new AudioExportContext(context);
#else
            Debug.LogWarning("Audio export is only supported in the Unity Editor.");
            return null;    
#endif
        }
    }
    
#if UNITY_EDITOR 
    public class AudioExportContext: GLTFExportPluginContext
    {
        private List<AudioDescription> _audioSourceIds = new();
        private KHR_audio_emitter _audioExtension; 
        private KHR_SceneAudioEmittersRef _sceneExtension = null;

        private Dictionary<AudioSource, AudioEmitterId> _audioSourceToEmitter = new();
        private Dictionary<AudioSource, Node> _audioSourceToNode = new();
        
        public class AudioDescription
        {
            public int Id;
            public string Name;
            public AudioClip Clip;
        }
        
        public AudioExportContext(ExportContext context)
        {
        }

        private AudioDescription AddAudioSource(AudioSource audioSource, out bool isNew)
        {
            AudioClip clip = audioSource.clip;
            string name = clip.name;

            foreach (var a in _audioSourceIds)
            {
                if (name == a.Name && clip == a.Clip)
                {
                    isNew = false;
                    return a;
                }
            }
            AudioDescription ad = new AudioDescription() { Id = _audioSourceIds.Count, Name = clip.name, Clip = clip };
            _audioSourceIds.Add(ad);
            isNew = true;
            return ad;
        }

        private string GetMimeType(string path)
        {
            var extension = Path.GetExtension(path);
            if (extension == ".mp3")
                return "audio/mpeg";
            if (extension == ".ogg")
                return "audio/ogg";
            if (extension == ".wav")
                return "audio/wav";
            return null;
        }
        
        private AudioEmitterId ProcessAudioSource(bool isGlobal, AudioSource[] audioSources, GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            var audioSourceIds = new List<AudioDescription>();
            var exportRequired = new List<AudioDescription>();
            foreach (var source in audioSources)
            {
                var audioDescription = AddAudioSource(source, out var isNew);
                audioSourceIds.Add(audioDescription);
                if (isNew)
                    exportRequired.Add(audioDescription);
            }

            var firstAudioSource = audioSources[0];


            var emitter = new KHR_AudioEmitter
            {
                type = isGlobal ? "global" : "positional",
                gain = firstAudioSource.volume,
                name = isGlobal ? "global emitter" : "positional emitter"
            };
            
            if (!isGlobal)
            {
                emitter.positional = new PositionalEmitterData()
                {
                    refDistance = firstAudioSource.minDistance,
                    maxDistance = firstAudioSource.maxDistance,
                    distanceModel = PositionalAudioDistanceModel.linear,
                };
            }
            emitter.sources.AddRange(audioSourceIds.Select(a => new AudioSourceId { Id = a.Id, Root = gltfRoot }));

            _audioExtension.emitters.Add(emitter);

            foreach (var audioSourceId in exportRequired)
            {
                var clip = audioSourceId.Clip;

                var path = AssetDatabase.GetAssetPath(clip);

                var fileName = Path.GetFileName(path);
             
                var audio = new KHR_AudioData();
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                var mimeType = GetMimeType(fileName);
                if (string.IsNullOrEmpty(mimeType))
                {
                    Debug.LogError("Unsupported audio file type: " + fileName);
                    continue;
                }
                var result = exporter.ExportFile(fileName, mimeType, fileStream);
                
                if (string.IsNullOrEmpty(result.uri))
                {
                    audio.mimeType = result.mimeType;
                    audio.bufferView = result.bufferView;
                }
                else
                {
                    audio.uri = result.uri;
                }
                _audioExtension.audio.Add(audio);
            }
            
            foreach (var audioSourceId in audioSourceIds)
            {
                var khrAudio = new KHR_AudioSource
                {
                    audio = new AudioDataId { Id = audioSourceId.Id, Root = gltfRoot },
                    autoPlay = firstAudioSource.playOnAwake,
                    loop = firstAudioSource.loop,
                    gain = firstAudioSource.volume,
                    // TODO: uniquename required?
                    Name = audioSourceId.Clip.name
                };

                _audioExtension.sources.Add(khrAudio);
            }

            var ermitterId = new AudioEmitterId() { Id = _audioExtension.emitters.Count - 1, Root = gltfRoot };
     
            return ermitterId;
        }
        
        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
        {
            var audioSources = transform.GetComponents<AudioSource>();
            if (audioSources.Length == 0)
                return;

            var globalSources = new List<AudioSource>( audioSources.Where( a => a.spatialBlend < 0.5f) );
            var positionalSources = new List<AudioSource>( audioSources.Where( a => a.spatialBlend >= 0.5f) );
            
            if (_audioExtension == null)
            {
                _audioExtension = new KHR_audio_emitter();
                if (gltfRoot != null)
                {
                    gltfRoot.AddExtension(KHR_audio_emitter.ExtensionName, _audioExtension);
                    exporter.DeclareExtensionUsage(KHR_audio_emitter.ExtensionName);
                }
            }
            
            // TODO: check if audio source settings are the same, otherwise add the source separately and create child nodes for the ermitter
            // We try to export multiple audio source in a single ermitter with multiple sources

            if (positionalSources.Count > 0)
            {
                var ermitterId = ProcessAudioSource(false, positionalSources.ToArray(), exporter, gltfRoot);
                if (ermitterId == null)
                    return;
                foreach (var a in positionalSources)
                {
                    _audioSourceToEmitter.Add(a, ermitterId);
                    _audioSourceToNode.Add(a, node);
                }

                var nodeErmitter = new KHR_NodeAudioEmitterRef();
                nodeErmitter.emitter = ermitterId;
                node.AddExtension(KHR_NodeAudioEmitterRef.ExtensionName, nodeErmitter);
            }

            if (globalSources.Count > 0)
            {
                var ermitterId = ProcessAudioSource(true, globalSources.ToArray(), exporter, gltfRoot);
                if (ermitterId == null)
                    return;

                foreach (var a in globalSources)
                    _audioSourceToEmitter.Add(a, ermitterId);
                
                if (_sceneExtension == null)
                     _sceneExtension = new KHR_SceneAudioEmittersRef();

                _sceneExtension.emitters.Add(ermitterId);
            }
            
        }

        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            if (_sceneExtension == null)
                return;
            
            gltfRoot.Scenes[0].AddExtension(KHR_SceneAudioEmittersRef.ExtensionName, _sceneExtension);
        }
    }
#endif
}
