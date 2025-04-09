using System;
using System.Globalization;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetParentUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT =
            "Export is currently only supported with a literal/default input or a This as input.";
        
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.parent),
                new Transform_GetParentUnitExporter());
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
            
            var varId = unitExporter.vsExportContext.AddVariableWithIdIfNeeded("_PARENT_TRANSFORM_"+targetIndex.ToString(), targetIndex, VariableKind.Scene, typeof(int));
            VariablesHelpers.GetVariable(unitExporter, varId, out var socket);
            socket.MapToPort(unit.value).ExpectedType(ExpectedType.Int);
            
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