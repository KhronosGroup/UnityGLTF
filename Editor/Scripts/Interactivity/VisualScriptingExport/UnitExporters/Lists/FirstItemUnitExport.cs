using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class FirstItemUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(FirstItem); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new FirstItemUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as FirstItem;
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            VariablesHelpers.GetVariable(unitExporter, list.StartIndex, out var startIndex);
            startIndex.MapToPort(unit.firstItem);
            return true;
        }
    }
}