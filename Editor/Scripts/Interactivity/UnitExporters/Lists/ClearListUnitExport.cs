using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace Editor.UnitExporters.Lists
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
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                Debug.LogError("Could not find list for ClearList unit");
                return false;
            }
            
            ListHelpers.ClearList(unitExporter, list, unit.enter, unit.exit);
            unitExporter.ByPassValue(unit.listInput, unit.listOutput);
            return true;
        }
    }
}