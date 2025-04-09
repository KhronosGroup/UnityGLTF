using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ClearListUnitExport: IUnitExporter 
    {
        public Type unitType { get => typeof(ClearList); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ClearListUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ClearList;
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpersVS.ClearList(unitExporter, list, unit.enter, unit.exit);
            unitExporter.ByPassValue(unit.listInput, unit.listOutput);

            return true;
        }
    }
}