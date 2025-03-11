using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace Editor.UnitExporters.Lists
{
    public class LastItemUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(LastItem); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new LastItemUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as LastItem;

            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                Debug.LogError("Could not find list for LastItem unit");
                return false;
            }
            
            var subNode = unitExporter.CreateNode(new Math_SubNode());
            subNode.ValueIn("a").ConnectToSource(ListHelpers.GetListCountSocket(list));
            subNode.ValueIn("b").SetValue(1);
            subNode.ValueOut("value").ExpectedType(ExpectedType.Int);
            
            ListHelpers.GetItem(unitExporter, list, subNode.ValueOut("value"), out var itemValue);
            itemValue.MapToPort(unit.lastItem);
            return true;
        }
    }
}