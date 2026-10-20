using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// AudioSource.Play, Stop, Pause and UnPause as audio/* ops declared with KHR_audio_emitter
    /// (the draft has no interactivity nodes yet, see <see cref="Audio_PlayNode"/>).
    /// </summary>
    public class AudioSource_PlaybackUnitExport : IUnitExporter
    {
        public Type unitType => typeof(InvokeMember);

        private readonly Type schemaType;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.Play), new AudioSource_PlaybackUnitExport(typeof(Audio_PlayNode)));
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.Stop), new AudioSource_PlaybackUnitExport(typeof(Audio_StopNode)));
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.Pause), new AudioSource_PlaybackUnitExport(typeof(Audio_PauseNode)));
            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource.UnPause), new AudioSource_PlaybackUnitExport(typeof(Audio_UnpauseNode)));
        }

        private AudioSource_PlaybackUnitExport(Type schemaType)
        {
            this.schemaType = schemaType;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            if (unit.inputParameters.Count > 0)
            {
                UnitExportLogging.AddErrorLog(unit, "AudioSource.Play with a delay is not supported.");
                return false;
            }

            if (!AudioHelperVS.TryGetExportedAudioSource(unitExporter, unit.target, out var audioSource, out var audioExport))
                return false;

            // The socket ids are the same for all audio/* ops
            var node = unitExporter.CreateNode(schemaType);
            node.ValueIn(Audio_PlayNode.IdValueSourceRef).SetValue(AudioHelperVS.SourceRef(audioExport.GetSourceIndex(audioSource)))
                .SetType(TypeRestriction.LimitToRef);
            node.FlowIn(Audio_PlayNode.IdFlowIn).MapToControlInput(unit.enter);
            node.FlowOut(Audio_PlayNode.IdFlowOut).MapToControlOutput(unit.exit);
            return true;
        }
    }

    /// <summary>
    /// AudioSource properties as KHR_audio_emitter pointers: volume, loop and pitch of the source,
    /// minDistance and maxDistance of the positional emitter.
    /// </summary>
    public abstract class AudioSourceMemberUnitExport : IUnitExporter
    {
        protected static readonly string[] Members =
        {
            nameof(AudioSource.volume), nameof(AudioSource.loop), nameof(AudioSource.pitch),
            nameof(AudioSource.minDistance), nameof(AudioSource.maxDistance),
        };

        protected readonly string member;

        public abstract Type unitType { get; }

        protected AudioSourceMemberUnitExport(string member)
        {
            this.member = member;
        }

        public abstract bool InitializeInteractivityNodes(UnitExporter unitExporter);

        protected bool SetupPointer(UnitExporter unitExporter, GltfInteractivityExportNode pointerNode, ValueInput target, out string gltfType)
        {
            gltfType = GltfTypes.Float;
            if (!AudioHelperVS.TryGetExportedAudioSource(unitExporter, target, out var audioSource, out var audioExport))
                return false;

            string pointerId, template;
            StaticRefPointer targetRef;
            switch (member)
            {
                case nameof(AudioSource.minDistance):
                case nameof(AudioSource.maxDistance):
                    if (!audioExport.IsPositional(audioSource))
                    {
                        UnitExportLogging.AddErrorLog(unitExporter.unit, $"AudioSource.{member} is only supported on positional AudioSources (spatialBlend >= 0.5).");
                        return false;
                    }
                    pointerId = PointersHelper.IdPointerAudioEmitterRef;
                    template = PointersHelper.IdPointerTemplAudioEmitterByRef + (member == nameof(AudioSource.minDistance) ? "/positional/refDistance" : "/positional/maxDistance");
                    targetRef = AudioHelperVS.EmitterRef(audioExport.GetEmitterIndex(audioSource));
                    break;
                default:
                    pointerId = PointersHelper.IdPointerAudioSourceRef;
                    template = PointersHelper.IdPointerTemplAudioSourceByRef;
                    switch (member)
                    {
                        case nameof(AudioSource.volume):
                            template += "/gain";
                            break;
                        case nameof(AudioSource.loop):
                            template += "/loop";
                            gltfType = GltfTypes.Bool;
                            break;
                        case nameof(AudioSource.pitch):
                            template += "/playbackRate";
                            break;
                    }
                    targetRef = AudioHelperVS.SourceRef(audioExport.GetSourceIndex(audioSource));
                    break;
            }

            PointersHelper.SetupPointerTemplateAndTargetInput(pointerNode, pointerId, template, gltfType);
            pointerNode.ValueIn(pointerId).SetValue(targetRef).SetType(TypeRestriction.LimitToRef);
            return true;
        }
    }

    public class AudioSource_GetMemberUnitExport : AudioSourceMemberUnitExport
    {
        public override Type unitType => typeof(GetMember);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            foreach (var member in Members)
                GetMemberUnitExport.RegisterMemberExporter(typeof(AudioSource), member, new AudioSource_GetMemberUnitExport(member));
        }

        private AudioSource_GetMemberUnitExport(string member) : base(member)
        {
        }

        public override bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            var node = unitExporter.CreateNode<Pointer_GetNode>();
            if (!SetupPointer(unitExporter, node, unit.target, out var gltfType))
                return false;

            node.FirstValueOut().ExpectedType(ExpectedType.GtlfType(gltfType)).MapToPort(unit.value);
            return true;
        }
    }

    public class AudioSource_SetMemberUnitExport : AudioSourceMemberUnitExport
    {
        public override Type unitType => typeof(SetMember);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            foreach (var member in Members)
                SetMemberUnitExport.RegisterMemberExporter(typeof(AudioSource), member, new AudioSource_SetMemberUnitExport(member));
        }

        private AudioSource_SetMemberUnitExport(string member) : base(member)
        {
        }

        public override bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetMember;
            var node = unitExporter.CreateNode<Pointer_SetNode>();
            if (!SetupPointer(unitExporter, node, unit.target, out var gltfType))
                return false;

            node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.input).SetType(TypeRestriction.LimitToType(gltfType));
            node.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.assign);
            node.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.assigned);
            unitExporter.ByPassValue(unit.input, unit.output);
            return true;
        }
    }
}
