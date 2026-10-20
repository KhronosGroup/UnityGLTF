using System.Linq;
using GLTF.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Resolves AudioSources to the sources and emitters exported by the KHR_audio_emitter export plugin.
    /// </summary>
    public static class AudioHelperVS
    {
        /// <summary>
        /// The AudioSource of target and the audio export plugin. The AudioSource must be known at export time
        /// (literal, default value or self): the glTF object model has no pointer from a node to its audio emitter.
        /// </summary>
        public static bool TryGetExportedAudioSource(UnitExporter unitExporter, ValueInput target, out AudioSource audioSource, out AudioExportContext audioExport)
        {
            audioSource = null;
            audioExport = null;
            var unit = unitExporter.unit;
            if (!unitExporter.IsInputLiteralOrDefaultValue(target, out var value))
            {
                UnitExportLogging.AddErrorLog(unit, "The AudioSource must be known at export time (a literal, default value or This), dynamic AudioSource references are not supported.");
                return false;
            }

            AudioSource[] audioSources = null;
            switch (value)
            {
                case AudioSource a:
                    audioSource = a;
                    break;
                case GameObject go:
                    audioSources = go.GetComponents<AudioSource>();
                    break;
                case Component component:
                    audioSources = component.GetComponents<AudioSource>();
                    break;
            }

            if (audioSources != null && audioSources.Length > 0)
            {
                audioSource = audioSources[0];
                if (audioSources.Length > 1)
                    UnitExportLogging.AddWarningLog(unit, $"{audioSource.name} has {audioSources.Length} AudioSources, the first one is used. Reference the AudioSource component to use another one.");
            }

            if (audioSource == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Target has no AudioSource component.");
                return false;
            }

            audioExport = unitExporter.vsExportContext.exporter.Plugins.OfType<AudioExportContext>().FirstOrDefault();
            if (audioExport == null)
            {
                UnitExportLogging.AddErrorLog(unit, $"AudioSources are only exported with the {KHR_audio_emitter.ExtensionName} export plugin, which is disabled.");
                return false;
            }

            if (audioExport.GetSourceIndex(audioSource) == -1)
            {
                UnitExportLogging.AddErrorLog(unit, $"AudioSource on {audioSource.name} is not exported.");
                return false;
            }
            return true;
        }

        public static StaticRefPointer SourceRef(int sourceIndex)
        {
            return new StaticRefPointer($"/extensions/{KHR_audio_emitter.ExtensionName}/sources/{sourceIndex}");
        }

        public static StaticRefPointer EmitterRef(int emitterIndex)
        {
            return new StaticRefPointer($"/extensions/{KHR_audio_emitter.ExtensionName}/emitters/{emitterIndex}");
        }
    }
}
