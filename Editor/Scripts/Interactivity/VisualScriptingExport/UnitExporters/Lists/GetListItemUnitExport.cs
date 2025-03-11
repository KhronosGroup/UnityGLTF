using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport;

namespace Editor.UnitExporters.Lists
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
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                Debug.LogError("Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpers.GetItem(unitExporter, list, unit.index, out var socket);
            socket.MapToPort(unit.item);
            return true;
        }
    }
}