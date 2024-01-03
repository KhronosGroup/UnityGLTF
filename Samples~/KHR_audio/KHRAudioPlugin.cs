#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Schema;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF.Plugins.Experimental
{
    public class KHRAudioPlugin : GLTFExportPlugin
    {
        public override string DisplayName => "KHR_audio";
        public override string Description => "Exports positional and global audio sources and .mp3 audio clips. Currently requires adding \"KHRPositionalAudioEmitterBehavior\" and \"KHRGlobalAudioEmitterBehavior\" components to scene objects.";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new AudioExtensionConfig();
        }
    }
    public class AudioExtensionConfig: GLTFExportPluginContext
    {
        static List<AudioClip> audioDataClips = new List<AudioClip>();
        static List<AudioSourceScriptableObject> audioSourceObjects = new List<AudioSourceScriptableObject>();
        static List<KHR_AudioEmitter> audioEmitters = new List<KHR_AudioEmitter>();

        public override void AfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
        {
            var audioEmitterBehavior = transform.GetComponent<KHRPositionalAudioEmitterBehaviour>();

            if (audioEmitterBehavior != null)
            {
                var audioSourceIds = AddAudioSources(gltfRoot, audioEmitterBehavior.sources);

                var emitterId = new AudioEmitterId
                {
                    Id = audioEmitters.Count,
                    Root = gltfRoot
                };

                var emitter = new KHR_PositionalAudioEmitter
                {
                    type = "positional",
                    sources = audioSourceIds,
                    gain = audioEmitterBehavior.gain,
                    coneInnerAngle = audioEmitterBehavior.coneInnerAngle * Mathf.Deg2Rad,
                    coneOuterAngle = audioEmitterBehavior.coneOuterAngle * Mathf.Deg2Rad,
                    coneOuterGain = audioEmitterBehavior.coneOuterGain,
                    distanceModel = audioEmitterBehavior.distanceModel,
                    refDistance = audioEmitterBehavior.refDistance,
                    maxDistance = audioEmitterBehavior.maxDistance,
                    rolloffFactor = audioEmitterBehavior.rolloffFactor
                };

                audioEmitters.Add(emitter);

                var extension = new KHR_NodeAudioEmitterRef
                {
                    emitter = emitterId
                };

                node.AddExtension(KHR_audio.ExtensionName, extension);
                exporter.DeclareExtensionUsage(KHR_audio.ExtensionName);
            }
        }

        public override void AfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
        {
            var globalEmitterBehaviors = Object.FindObjectsOfType<KHRGlobalAudioEmitterBehaviour>();

            if (globalEmitterBehaviors.Length > 0)
            {
                var globalEmitterIds = new List<AudioEmitterId>();

                foreach (var emitterBehavior in globalEmitterBehaviors)
                {
                    var audioSourceIds = AddAudioSources(gltfRoot, emitterBehavior.sources);

                    var emitterId = new AudioEmitterId
                    {
                        Id = audioEmitters.Count,
                        Root = gltfRoot
                    };

                    globalEmitterIds.Add(emitterId);

                    var globalEmitter = new KHR_AudioEmitter
                    {
                        type = "global",
                        sources = audioSourceIds,
                        gain = emitterBehavior.gain
                    };

                    audioEmitters.Add(globalEmitter);
                }

                var extension = new KHR_SceneAudioEmittersRef
                {
                    emitters = globalEmitterIds
                };

                var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];

                scene.AddExtension(KHR_audio.ExtensionName, extension);
                exporter.DeclareExtensionUsage(KHR_audio.ExtensionName);
            }

            if (audioEmitters.Count > 0)
            {
                var audioData = new List<KHR_AudioData>();

                for (int i = 0; i < audioDataClips.Count; i++)
                {
                    var audioClip = audioDataClips[i];

                    var path = AssetDatabase.GetAssetPath(audioClip.GetInstanceID());

                    var fileExtension = Path.GetExtension(path);

                    if (fileExtension != ".mp3")
                    {
                        audioDataClips.Clear();
                        audioSourceObjects.Clear();
                        audioEmitters.Clear();
                        throw new Exception("Unsupported audio file type \"" + fileExtension + "\", only .mp3 is supported.");
                    }

                    var fileName = Path.GetFileName(path);
                    var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    var result = exporter.ExportFile(fileName, "audio/mpeg", fileStream);
                    var audio = new KHR_AudioData
                    {
                        uri = result.uri,
                        mimeType = result.mimeType,
                        bufferView = result.bufferView,
                    };

                    audioData.Add(audio);
                }

                var audioSources = new List<KHR_AudioSource>();

                for (int i = 0; i < audioSourceObjects.Count; i++)
                {
                    var audioSourceObject = audioSourceObjects[i];
                    var audioDataIndex = audioDataClips.IndexOf(audioSourceObject.clip);

                    var audioSource = new KHR_AudioSource
                    {
                        audio = audioDataIndex == -1 ? null : new AudioDataId { Id = audioDataIndex, Root = gltfRoot },
                        autoPlay = audioSourceObject.autoPlay,
                        loop = audioSourceObject.loop,
                        gain = audioSourceObject.gain,
                    };

                    audioSources.Add(audioSource);
                }

                var extension = new KHR_audio
                {
                    audio = new List<KHR_AudioData>(audioData),
                    sources = new List<KHR_AudioSource>(audioSources),
                    emitters = new List<KHR_AudioEmitter>(audioEmitters),
                };

                gltfRoot.AddExtension(KHR_audio.ExtensionName, extension);
            }

            audioDataClips.Clear();
            audioSourceObjects.Clear();
            audioEmitters.Clear();
        }

        private static List<AudioSourceId> AddAudioSources(GLTFRoot gltfRoot, List<AudioSourceScriptableObject> sources)
        {
            var audioSourceIds = new List<AudioSourceId>();

            foreach (var audioSource in sources)
            {
                var audioSourceIndex = audioSourceObjects.IndexOf(audioSource);

                if (audioSourceIndex == -1)
                {
                    audioSourceIndex = audioSourceObjects.Count;
                    audioSourceObjects.Add(audioSource);
                }

                if (!audioDataClips.Contains(audioSource.clip))
                {
                    audioDataClips.Add(audioSource.clip);
                }

                var sourceId = new AudioSourceId
                {
                    Id = audioSourceIndex,
                    Root = gltfRoot
                };

                audioSourceIds.Add(sourceId);
            }

            return audioSourceIds;
        }
    }

    public enum PositionalAudioDistanceModel
    {
        linear,
        inverse,
        exponential,
    }
}

#endif