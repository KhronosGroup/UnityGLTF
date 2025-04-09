using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetChildCountUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT =
            "Export is currently only supported with a literal/default input or a This as target input.";

        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.childCount), new Transform_GetChildCountUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            
            GameObject target = UnitsHelper.GetGameObjectFromValueInput(unit.target, unit.defaultValues, unitExporter.vsExportContext);

            if (target ==null)
            {
                UnitExportLogging.AddErrorLog(unit, "Could not resolve target GameObject.");
                return false;
            }
            int targetIndex = unitExporter.vsExportContext.exporter.GetTransformIndex(target.transform);
            if (targetIndex == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Could not resolve target GameObject.");
                return false;
            }
            
            var list = unitExporter.vsExportContext.GetListByName($"CHILD_LIST_FROM_TRANSFORM_{targetIndex}");
            if (list != null)
            {
                // When we have a child list of the target, we can use the count of the list
                ListHelpersVS.GetListCountSocket(list).MapToPort(unit.value);
                return true;
            }

            var childCount = target.transform.childCount;
            var varId = unitExporter.vsExportContext.AddVariableWithIdIfNeeded($"CHILD_COUNT_FROM_TRANSFORM_{targetIndex}", childCount, VariableKind.Scene, typeof(int));
            VariablesHelpers.GetVariable(unitExporter, varId, out var valueSocket);
            valueSocket.MapToPort(unit.value);
            return true;
        }
        
        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add(WARNING_TEXT);
            return logs;
        }
    }
}