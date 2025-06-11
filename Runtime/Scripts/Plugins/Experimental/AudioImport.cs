using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityGLTF.Plugins
{
    [NonRatifiedPlugin]
    public class AudioImport : GLTFImportPlugin
    {
        public override bool EnabledByDefault => false;
        public override string DisplayName => "KHR_audio_emitter";
        public override string Description => "Import positional and global audio sources (Wav, Mp3, Ogg)";
        
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new AudioImportContext(context);
        }
    }
    
#if UNITY_EDITOR
    
    // In OnPostprocessAllAssets, we have now the possibility to load the AudioClips from the asset path
    // and can assign them to the AudioSources in the Gltf Prefab
    internal class AudioImportPostprocessor : AssetPostprocessor
    {
        internal static List<string> lastImportedGltfs = new();

        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (var lastImportedGltf in lastImportedGltfs)
            {
                var gltfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lastImportedGltf);
                var assignClipComponent = gltfPrefab.GetComponentsInChildren<TempAssignClip>();
                var importer = AssetImporter.GetAtPath(lastImportedGltf);
                foreach (var assignClip in assignClipComponent)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assignClip.audioPath);
                    var audioSourceComponent = assignClip.GetComponents<AudioSource>();
                    audioSourceComponent[assignClip.audioSourceIndex].clip = clip;
                }

                foreach (var ac in assignClipComponent)
                    GameObject.DestroyImmediate(ac, true);

                EditorUtility.SetDirty(gltfPrefab);
            }

            lastImportedGltfs.Clear();
        }
    }
#endif 
    
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
        private Dictionary<int, string> _audioPaths = new();
        
        private string _audioFilesDestinationPath;
        
        public AudioImportContext(GLTFImportContext context) 
        {
            _context = context;
            
#if UNITY_EDITOR
            if (_context.AssetContext != null)
            {
                AudioImportPostprocessor.lastImportedGltfs.Add(_context.AssetContext.assetPath);
                var filenameWithOutExtension = Path.GetFileNameWithoutExtension(_context.AssetContext.assetPath);
                _audioFilesDestinationPath = Path.Combine(Path.GetDirectoryName(_context.AssetContext.assetPath), filenameWithOutExtension + "_Audio");
            }
#endif
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
#if UNITY_EDITOR
                    if (_context.AssetContext != null)
                    {
                        // When importing the gltf file as an asset, the audio files are not imported yet by Unity. 
                        // So we add the temporary Component TempAssignClip In OnPostprocessAllAssets, we will assign the audio clips to the AudioSources.
                        if (_audioPaths.TryGetValue(ac.AudioDataId.Id, out var audioPath))
                        {
                            audioPath = audioPath.Replace(@"\", "/");
                            var assignClip =ac.audioSource.gameObject.AddComponent<TempAssignClip>();
                            assignClip.audioPath = audioPath;
                            var sources = ac.audioSource.gameObject.GetComponents<AudioSource>();
                            assignClip.audioSourceIndex = Array.IndexOf(sources, ac.audioSource);
                        }
                        continue;
                    }
#endif
                    
                    Debug.LogWarning($"Audio clip not found for AudioDataId {ac.AudioDataId}");
                }
                
                // In case we load a gltf at runtime, and the Scene GameObject is already active, the playOnAwake will not
                // be called, so we need to call it manually
                if (ac.audioSource.playOnAwake && ac.audioSource.gameObject.activeInHierarchy
#if UNITY_EDITOR
                                               && _context.AssetContext == null
 #endif
                   )
                {
                    ac.audioSource.Play();
                }
            }
            
        }

        private string GetFileExtensionForMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case "audio/mpeg":
                    return ".mp3";
                case "audio/wav":
                    return ".wav";
                case "audio/ogg":
                    return ".ogg";
                default:
                    Debug.LogWarning($"Unsupported audio mime type: {mimeType}");
                    return null;
            }
        }
        
        private AudioType GetAudioTypeForMimeType(string mimeType)
        {
            switch (mimeType)
            {
                case "audio/mpeg":
                    return AudioType.MPEG;
                case "audio/wav":
                    return AudioType.WAV;
                case "audio/ogg":
                    return AudioType.OGGVORBIS;
                default:
                    Debug.LogWarning($"Unsupported audio mime type: {mimeType}");
                    return AudioType.UNKNOWN;
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
                        continue;
                    
                    var mimeTypeFileExtension = GetFileExtensionForMimeType(audio.mimeType);
                    if (string.IsNullOrEmpty(mimeTypeFileExtension))
                        continue;

#if UNITY_EDITOR
                    
                    if (_context.AssetContext != null)
                    {
                        // When imported as an Asset:
                        var assetFilepath = Path.Combine(_audioFilesDestinationPath, $"audio_{index:D3}{mimeTypeFileExtension}");
                        if (!Directory.Exists(_audioFilesDestinationPath))
                            Directory.CreateDirectory(_audioFilesDestinationPath);
                        File.WriteAllBytes(assetFilepath, buffer.ToArray());
                  
                        AssetDatabase.ImportAsset(assetFilepath, ImportAssetOptions.ForceUpdate);
                        _audioPaths.Add(index, assetFilepath);
                        continue;
                    }
#endif
                    // Runtime loaded Gltf:
#if HAVE_WEBREQUESTAUDIO
                    var tempFile = Path.Combine(Application.temporaryCachePath, "gltfAudioImport"+ mimeTypeFileExtension);
                    File.WriteAllBytes(tempFile, buffer.ToArray());
                    
                    var audioClipRequest = UnityWebRequestMultimedia.GetAudioClip(tempFile, GetAudioTypeForMimeType(audio.mimeType));
                    audioClipRequest.SendWebRequest();
                    while (!audioClipRequest.isDone)
                    {
                        // Wait for the request to complete
                    }
                    
                    if (audioClipRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Cannot load audio clip for mimeType {audio.mimeType}: {audioClipRequest.error}");
                        continue;
                    }
                    
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(audioClipRequest);
                    if (clip == null || clip.samples == 0)
                    {
                        Debug.LogError($"Cannot load audio clip for mimeType {audio.mimeType}");
                        continue;
                    }
                    
                    clip.name = $"audio_{index:D3}";
                    
                    _audioClips.Add(index, clip);
#else
                    Debug.LogWarning($"Missing UnityWebRequestAudio Module! Audio import is not supported. Please enable the UnityWebRequestAudio Module in Package Manager to import audio files at runtime.");
#endif
                }
                else
                {
                    Debug.LogWarning($"Audio buffer view not found for {audio.Name}");
                }
            }
            
            _context.SceneImporter.GenericObjectReferences.AddRange(_audioClips.Select( kvp => kvp.Value).ToArray());
        }

        private void AddGlobalEmitters(GameObject sceneObject)
        {
            GLTFScene scene = null;
            if (_context.Root.Scene != null)
                scene = _context.Root.Scene.Value;
            else
                scene = _context.Root.Scenes[0];
            
            if (scene == null || scene.Extensions == null)
                return;
            
            if (!scene.Extensions.TryGetValue(KHR_audio_emitter.ExtensionName, out var extension))
                return;
            if (extension is KHR_SceneAudioEmittersRef audioEmitterRef)
            {
                if (audioEmitterRef.emitters != null)
                {
                    foreach (var emitter in audioEmitterRef.emitters)
                    {
                        if (emitter == null)
                            continue;
                        AddEmitter(emitter.Value, sceneObject, true);
                    }
                }
            }
        }

        private void AddEmitter(KHR_AudioEmitter emitter, GameObject toGameObject, bool isGlobal)
        {
            foreach (var source in emitter.sources)
            {
                if (source == null)
                    continue;
                        
                // TODO: set all parameters
                        
                var audioSource = toGameObject.AddComponent<AudioSource>();
                _assignClips.Add(new AssignClip(audioSource, source.Value.audio));
                audioSource.loop = source.Value.loop ?? false;
                audioSource.volume = emitter.gain * (source.Value.gain ?? 1f);
                audioSource.playOnAwake = source.Value.autoPlay ?? false;
                audioSource.spatialBlend = isGlobal ? 0f : 1.0f;
                if (!isGlobal && emitter.positional != null)
                {
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                    audioSource.minDistance = emitter.positional.refDistance ?? 1f;
                    audioSource.maxDistance = emitter.positional.maxDistance ?? 0f;
                }
            }
        }

        public override void OnAfterImportScene(GLTFScene scene, int sceneIndex, GameObject sceneObject)
        {
            if (_audioExtension == null)
                return;
            
            AddGlobalEmitters(sceneObject);
            CreateAudioClips();
            AssignClips();
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
                    AddEmitter(emitter, nodeObject, false);
                }
                else
                {
                    Debug.LogWarning($"Audio source not found for node {node.Name}");
                }
            }
            
        }
    }
}