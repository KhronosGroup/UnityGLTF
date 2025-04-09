using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GetListItemUnitExport :IUnitExporter
    {
        public Type unitType
        {
            get => typeof(GetListItem);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GetListItemUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetListItem;
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpersVS.GetItem(unitExporter, list, unit.index, out var socket);
            socket.MapToPort(unit.item);
            return true;
        }
    }
}