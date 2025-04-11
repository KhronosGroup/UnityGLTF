using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    [UnitExportPriority(ExportPriority.First)]
    public class CreateListUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public Type unitType { get => typeof(CreateList); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new CreateListUnitExport());
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as CreateList;

            var listCapacity = unit.inputCount;

            var firstInput = unit.validInputs.FirstOrDefault();
            if (firstInput == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }

            if (!unitExporter.IsInputLiteralOrDefaultValue(firstInput as ValueInput, out var value))
            {
                UnitExportLogging.AddErrorLog(unit, "Currently only literals are supported for list creation.");
                return false;
            }

            unitExporter.Context.ConvertValue(value, out _, out var valueTypeIndex);
            
            var objectList = unitExporter.vsExportContext.CreateNewVariableBasedListFromUnit(unit, listCapacity, valueTypeIndex);
            
            ListHelpersVS.CreateListNodes(unitExporter, objectList);
            
            foreach (var input in unit.validInputs)
            {
                if (unitExporter.IsInputLiteralOrDefaultValue(input as ValueInput, out var inputValue))
                {
                    unitExporter.Context.ConvertValue(inputValue, out var convertedValue, out var convertedTypeIndex);
                    objectList.AddItem(convertedValue);
                }
            }
            return true;
        }

        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add("Exported list capacity will be limited to the size: "+(unit as CreateList).inputCount);
            return logs;
        }
    }
}