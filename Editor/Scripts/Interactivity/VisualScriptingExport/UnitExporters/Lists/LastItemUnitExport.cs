using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
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

            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            
            var subNode = unitExporter.CreateNode<Math_SubNode>();
            subNode.ValueIn("a").ConnectToSource(ListHelpersVS.GetListCountSocket(list));
            subNode.ValueIn("b").SetValue(1);
            subNode.ValueOut("value").ExpectedType(ExpectedType.Int);
            
            ListHelpersVS.GetItem(unitExporter, list, subNode.ValueOut("value"), out var itemValue);
            itemValue.MapToPort(unit.lastItem);
            return true;
        }
    }
}