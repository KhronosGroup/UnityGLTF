using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using Unity.VisualScripting;
using System;
using UnityEditor;


namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class AudioSourceUnitExport : IUnitExporter
    {
        //public Type unitType { get => typeof(AudioSource); }
        public System.Type unitType
        {
            get => typeof(UnityEngine.AudioSource);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
//            InvokeUnitExport.RegisterInvokeExporter(typeof(AudioSource), nameof(AudioSource),
//                new AudioSourceUnitExport());
            UnitExporterRegistry.RegisterExporter(new AudioSourceUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            return true;
            /*
            var unit = unitExporter.unit as AudioSource;

            if (unit.target == null)
                return false;

            // Regular pointer/set

            var materialTemplate = "/materials/{" + GltfInteractivityNodeHelper.IdPointerMaterialIndex + "}/";
            var template = materialTemplate + "pbrMetallicRoughness/baseColorFactor";

            if (unit is SetMember setMember)
            {
                var node = unitExporter.CreateNode(new Pointer_SetNode());
                unitExporter.MapInputPortToSocketName(setMember.assign, Pointer_SetNode.IdFlowIn, node);
                unitExporter.MapInputPortToSocketName(setMember.input, Pointer_SetNode.IdValue, node);
                unitExporter.MapOutFlowConnectionWhenValid(setMember.assigned, Pointer_SetNode.IdFlowOut, node);

                node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMaterialIndex,
                    setMember.target, template, GltfTypes.Float4);
            }
            else if (unit is InvokeMember invokeMember)
            {
                // first parameter is the color property name – so based on that we can determine what pointer to set
                // var colorPropertyName = invokeMember.inputParameters[0];
                bool hasAlpha = true;
                if (unitExporter.IsInputLiteralOrDefaultValue(invokeMember.inputParameters[0], out var colorPropertyName))
                {
                    var gltfProperty = MaterialPointerHelper.GetPointer(unitExporter, (string)colorPropertyName, out var map);
                    if (gltfProperty == null)
                    {
                        UnitExportLogging.AddErrorLog(unit, "color property name is not supported.");
                        return false;
                    }

                    hasAlpha = map.ExportKeepColorAlpha;
                    template = materialTemplate + gltfProperty;
                }
                else
                {
                    UnitExportLogging.AddErrorLog(unit, "color property name is not a literal or default value, which is not supported.");
                    return false;
                }

                var node = unitExporter.CreateNode(new Pointer_SetNode());
                node.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(invokeMember.enter);
                node.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(invokeMember.exit);

                if (hasAlpha)
                {
                    node.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(invokeMember.inputParameters[1]).SetType(TypeRestriction.LimitToFloat4);

                    node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float4);
                }
                else
                {
                    var extract = unitExporter.CreateNode(new Math_Extract4Node());
                    var combine = unitExporter.CreateNode(new Math_Combine3Node());
                    extract.ValueIn("a").MapToInputPort(invokeMember.inputParameters[1])
                        .SetType(TypeRestriction.LimitToFloat4);

                    combine.ValueIn("a").ConnectToSource(extract.ValueOut("0"));
                    combine.ValueIn("b").ConnectToSource(extract.ValueOut("1"));
                    combine.ValueIn("c").ConnectToSource(extract.ValueOut("2"));

                    node.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(combine.FirstValueOut());
                    node.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMaterialIndex,
                        invokeMember.target, template, GltfTypes.Float3);
                }
            }
            return true;
            */
        }
    }

}
