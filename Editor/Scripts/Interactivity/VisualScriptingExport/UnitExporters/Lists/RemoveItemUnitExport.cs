using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace Editor.UnitExporters.Lists
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
            
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                Debug.LogError("Could not find list for RemoveListItemAt unit");
                return false;
            }
            
            ListHelpers.RemoveListItemAt(unitExporter, list, unit.index, unit.enter, unit.exit);
            return true;
        }
    }
}