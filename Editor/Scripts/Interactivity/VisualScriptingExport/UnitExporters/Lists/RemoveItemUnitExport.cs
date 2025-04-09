using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class RemoveItemUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(RemoveListItemAt); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new RemoveItemUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as RemoveListItemAt;
            
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpersVS.RemoveListItemAt(unitExporter, list, unit.index, unit.enter, unit.exit);
            return true;
        }
    }
}