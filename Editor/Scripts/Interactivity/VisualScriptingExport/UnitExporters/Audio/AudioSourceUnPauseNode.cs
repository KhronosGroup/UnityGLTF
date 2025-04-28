using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Unit Exporte for audio source unpause node
    /// </summary>
    public class AudioSource_UnPauseUnitExport : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }
		
        /// <summary>
        /// Register the instance of the unpause audio exporter
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.UnPause),
                new AudioSource_UnPauseUnitExport());
        }

        /// <summary>
        /// Sets up the unpause audio unitexporter with the correct data and associations
        /// </summary>
        /// <param name="unitExporter"></param>
        /// <returns></returns>
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            InvokeMember unit = unitExporter.unit as InvokeMember;

            GameObject target = UnitsHelper.GetGameObjectFromValueInput(
                unit.target, unit.defaultValues, unitExporter.exportContext);

            if (target == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve target GameObject");
                return false;
            }

            var audio = target.GetComponent<AudioSource>();
            if (!audio)
            {
                UnitExportLogging.AddErrorLog(unit, "Target GameObject does not have an Audio component.");
                return false;
            }

            var clip = audio.clip;
            if (clip != null)
            {
                var name = clip.name;

                GLTFAudioExportContext.AudioDescription description = GLTFAudioExportContext.AddAudioSource(audio);

                var node = unitExporter.CreateNode(new Audio_UnPauseNode());
                node.ValueSocketConnectionData[Audio_UnPauseNode.IdValueAudio].Value = description.Id;
//                node.ValueSocketConnectionData[Audio_UnPauseNode.IdValueNode].Value = $"audio/node/{description.Id}";

                unitExporter.MapInputPortToSocketName(unit.enter, Audio_UnPauseNode.IdFlowIn, node);
                // There should only be one output flow from the Animator.Play node
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Audio_UnPauseNode.IdFlowOut, node);
            }

            return true;
        }
    }
}
