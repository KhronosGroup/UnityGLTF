using System.Collections.Generic;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityGLTF.Plugins
{
    public class AudioImport : GLTFImportPlugin
    {
        public override string DisplayName => "KHR_audio";
        public override string Description => "Import positional and global audio sources and .mp3 audio clips.";
        
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new AudioImportContext(context);
        }

    }
    
    public class AudioImportContext : GLTFImportPluginContext
    {
        private GLTFImportContext _context;
        private KHR_audio_emitter _audioExtension;
        
        private class AssignClip
        {
            public AudioSource audioSource;
            public AudioDataId AudioDataId;
            
            public AssignClip(AudioSource audioSource, AudioDataId audioDataId)
            {
                this.audioSource = audioSource;
                AudioDataId = audioDataId;
            }
        }
        
        private List<AssignClip> _assignClips = new();
        
        private Dictionary<int, AudioClip> _audioClips = new();
        
        public AudioImportContext(GLTFImportContext context) 
        {
            _context = context;
        }

        private void GetExtension(GLTFRoot root)
        {
            if (_audioExtension != null)
                return;

            if (root.Extensions == null)
                return;
            
            if (root.Extensions.TryGetValue(KHR_audio_emitter.ExtensionName, out var extension))
            {
                _audioExtension = extension as KHR_audio_emitter;
            }
            else
            {
                Debug.LogWarning($"Audio extension not found in GLTF root.");
            }
        }

        public override void OnAfterImportRoot(GLTFRoot gltfRoot)
        {
            GetExtension(gltfRoot);
        }

        private AudioClip GetAudioClip(KHR_AudioData audio)
        {
            return null;
        }

        private void AssignClips()
        {
            foreach (var ac in _assignClips)
            {
                if (_audioClips.TryGetValue(ac.AudioDataId.Id, out var audioClip))
                {
                    ac.audioSource.clip = audioClip;
                }
                else
                {
                    Debug.LogWarning($"Audio clip not found for AudioDataId {ac.AudioDataId}");
                }
            }
        }

        private void CreateAudioClips()
        {
            int index = -1;
            foreach (var audio in _audioExtension.audio)
            {
                index++;
                if (audio.bufferView != null)
                {
                    var buffer =  _context.SceneImporter.GetBufferViewData(audio.bufferView.Value);
                    if (buffer == null)
                    {
                        continue;
                    }
                    // TODO: save buffer as file and load it with UnityWebRequestMultimedia.GetAudioClip() 
                    AudioClip clip = null;
                    _audioClips.Add(index, clip);
                 
                }
                else
                {
                    Debug.LogWarning($"Audio buffer view not found for {audio.Name}");
                }
            }
            
            _context.SceneImporter.GenericObjectReferences.AddRange(_audioClips.Select( kvp => kvp.Value).ToArray());
        }

        public override void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
        {
            //TODO: when import as asset, we should create the audio clips in OnAfterImport to add the clips as subAssets
            CreateAudioClips();
            AssignClips();
        }

        public override void OnAfterImport()
        {
#if UNIT_EDITOR

            //
            // foreach (var audio in _audioClips)
            // {
            //     _context.AssetContext.AddObjectToAsset(audio.name, audio);
            // }

#endif            
        }

        public override void OnAfterImportNode(Node node, int nodeIndex, GameObject nodeObject)
        {
            if (_audioExtension == null)
                return;
            
            if (node.Extensions == null || !node.Extensions.TryGetValue(KHR_NodeAudioEmitterRef.ExtensionName, out var extension)) 
                return;

            if (extension is KHR_NodeAudioEmitterRef audioEmitterRef)
            {
                if (audioEmitterRef.emitter != null)
                {
                    var emitter = audioEmitterRef.emitter.Value;
                    foreach (var source in emitter.sources)
                    {
                        if (source == null)
                            continue;
                        
                        // TODO: set parameters
                        
                        var audioSource = nodeObject.AddComponent<AudioSource>();
                        _assignClips.Add(new AssignClip(audioSource, source.Value.audio));
                        audioSource.spatialBlend = 1.0f; // Set to 3D sound
                        audioSource.rolloffMode = AudioRolloffMode.Linear;
                        audioSource.minDistance = 1.0f;
                        audioSource.maxDistance = 100.0f;
                    }
                }
                else
                {
                    Debug.LogWarning($"Audio source not found for node {node.Name}");
                }
            }
            
        }
    }
}