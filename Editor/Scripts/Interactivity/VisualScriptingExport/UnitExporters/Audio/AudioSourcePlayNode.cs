using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity;
using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Plugins;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Unit Exporter for audio source play node
    /// </summary>
    public class AudioSource_PlayUnitExport : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }

        /// <summary>
        /// Register the instance of the play exporter
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.Play),
                new AudioSource_PlayUnitExport());
        }


        /// <summary>
        /// Sets up the unitexport with the correct data and associations
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

                var node = unitExporter.CreateNode(new Audio_StartNode());
                node.ValueSocketConnectionData[Audio_StartNode.IdValueAudio].Value = description.Id;
//                node.ValueSocketConnectionData[Audio_StartNode.IdValueNode].Value = $"audio/node/{description.Id}";

                unitExporter.MapInputPortToSocketName(unit.enter, Audio_StartNode.IdFlowIn, node);
                // There should only be one output flow from the Animator.Play node
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Audio_StartNode.IdFlowOut, node);

            }

            return true;
        }
    }
}
