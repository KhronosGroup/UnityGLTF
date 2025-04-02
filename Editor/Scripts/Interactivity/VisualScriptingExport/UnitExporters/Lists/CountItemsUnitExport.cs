using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class CountItemsUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(CountItems);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new CountItemsUnitExport()); 
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var countItems = unitExporter.unit as CountItems;
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, countItems);

            if (list == null)
            {
                UnitExportLogging.AddErrorLog(countItems, "Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpers.GetListCount(list, countItems.count);

            return true;
        }
    }
}