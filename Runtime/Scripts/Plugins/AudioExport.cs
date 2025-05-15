using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public class AudioExport: GLTFExportPlugin
    {
        public override string DisplayName => "KHR_audio";
        public override string Description => "Exports positional and global audio sources and .mp3 audio clips.";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new AudioExportContext(context);
        }
    }
    
    public class AudioExportContext: GLTFExportPluginContext
    {
        private ExportContext _context;
        private List<AudioDescription> _audioSourceIds = new();
        private KHR_audio _audioExtension; 
        private KHR_SceneAudioEmittersRef _sceneExtension = null;

        private bool _saveAudioToFile;
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
            _context = context;
            _saveAudioToFile = false;
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

            KHR_AudioEmitter emitter;
            if (isGlobal)
            {
                emitter = new KHR_AudioEmitter
                {
                    type = "global",
                    gain = firstAudioSource.volume,
                    name = "global emitter",
                };
            }
            else
            {
                emitter = new KHR_PositionalAudioEmitter()
                {
                    type = "positional",
                    gain = firstAudioSource.volume,
                    minDistance = firstAudioSource.minDistance,
                    maxDistance = firstAudioSource.maxDistance,
                    distanceModel = PositionalAudioDistanceModel.linear,
                    name = "positional emitter"
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
                var result = exporter.ExportFile(fileName, "audio/mpeg", fileStream);
                audio.mimeType = result.mimeType;
                audio.bufferView = result.bufferView;
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
                    name = audioSourceId.Clip.name
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
                _audioExtension = new KHR_audio();
                if (gltfRoot != null)
                {
                    gltfRoot.AddExtension(KHR_audio.ExtensionName, _audioExtension);
                    exporter.DeclareExtensionUsage(KHR_audio.ExtensionName);
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

}