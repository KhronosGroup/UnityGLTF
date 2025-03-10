using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace Editor.UnitExporters.Lists
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
                Debug.LogError("Can't resolve list detection by connections.");
                return false;
            }
            
            ListHelpers.GetListCount(list, countItems.count);
            return true;
        }
    }
}