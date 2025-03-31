using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class AudioSource_StopUnitExport : IUnitExporter
    {
        public System.Type unitType
        {
            get => typeof(InvokeMember);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.Stop),
                new AudioSource_StopUnitExport());
        }

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

                var node = unitExporter.CreateNode(new Audio_StopNode());
                node.ValueSocketConnectionData[Audio_StopNode.IdValueAudio].Value = description.Id;
                node.ValueSocketConnectionData[Audio_StopNode.IdValueNode].Value = $"audio/node/{description.Id}";

                unitExporter.MapInputPortToSocketName(unit.enter, Audio_StartNode.IdFlowIn, node);
                // There should only be one output flow from the Animator.Play node
                unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Audio_StartNode.IdFlowOut, node);
            }

            return true;
        }
    }
}
