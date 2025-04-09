using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class InsertListItemUnitExporter : IUnitExporter, IUnitExporterFeedback
    {
        public Type unitType { get => typeof(InsertListItem); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new InsertListItemUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InsertListItem;
            
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpersVS.InsertItem(unitExporter, list, unit.index, unit.item, unit.enter, unit.exit);
            unitExporter.ByPassValue(unit.listInput, unit.listOutput);
            return true;
        }
        
        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add("Be aware that exported lists will be limited in size to the capacity on export.");
            
            return logs;
        }
    }
}