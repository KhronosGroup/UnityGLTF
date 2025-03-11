using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Export;

namespace Editor.UnitExporters.Lists
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
                Debug.LogError("Could not find list for FirstItem unit");
                return false;
            }
            
            VariablesHelpers.GetVariable(unitExporter, list.StartIndex, out var startIndex);
            startIndex.MapToPort(unit.firstItem);
            return true;
        }
    }
}