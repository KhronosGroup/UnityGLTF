using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    [UnitExportPriority(ExportPriority.First)]
    public class Transform_GetChildUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        private const string WARNING_TEXT =
            "Export is currently only supported with a literal/default input or a This as target input.";

        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.GetChild)
                , new Transform_GetChildUnitExporter());
            
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
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
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            
            if (unitExporter.IsInputLiteralOrDefaultValue(unit.inputParameters[0], out var childIndexValueObj))
            {
                // When all is a static value: 
                int childIndex = (int)childIndexValueObj;
                var varId = unitExporter.vsExportContext.AddVariableWithIdIfNeeded($"CHILD_{childIndex}_FROM_TRANSFORM_{targetIndex}", childIndex, VariableKind.Scene, typeof(int));
                VariablesHelpers.GetVariable(unitExporter, varId, out var socket);
                socket.MapToPort(unit.result).ExpectedType(ExpectedType.Int);
                return true;
            }
            
            // When the input index is dynamic, we create a List of childs
            var childCount = target.transform.childCount;
            
            var childList = unitExporter.vsExportContext.GetListByName($"CHILD_LIST_FROM_TRANSFORM_{targetIndex}");
            if (childList == null)
            {
                childList = unitExporter.vsExportContext.CreateNewVariableBasedListFromUnit(unit, childCount,
                    GltfTypes.TypeIndex(typeof(int)), $"CHILD_LIST_FROM_TRANSFORM_{targetIndex}");
                
                ListHelpersVS.CreateListNodes(unitExporter, childList);
                for (int i = 0; i < childCount; i++)
                {
                    var childTransform = target.transform.GetChild(i);
                    var childIndex = unitExporter.vsExportContext.exporter.GetTransformIndex(childTransform);
                    childList.AddItem(childIndex);
                }
            }
            ListHelpersVS.GetItem(unitExporter, childList, unit.inputParameters[0], out var valueOutput);
            valueOutput.MapToPort(unit.result);
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