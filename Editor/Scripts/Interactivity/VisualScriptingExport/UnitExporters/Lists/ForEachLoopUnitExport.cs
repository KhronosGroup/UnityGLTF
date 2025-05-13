using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ForEachLoopUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(ForEach); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new ForEachLoopUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as ForEach;
            
            var list = ListHelpersVS.FindListByConnections(unitExporter.vsExportContext, unit);
            if (list == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Can't resolve list detection by connections.");
                return false;
            }
            var forLoop = unitExporter.CreateNode<Flow_ForLoopNode>();
            forLoop.Configuration[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
            forLoop.ValueIn(Flow_ForLoopNode.IdStartIndex).SetValue(0);

            forLoop.FlowIn(Flow_ForLoopNode.IdFlowIn).MapToControlInput(unit.enter);
            forLoop.FlowOut(Flow_ForLoopNode.IdLoopBody).MapToControlOutput(unit.body);
            forLoop.FlowOut(Flow_ForLoopNode.IdCompleted).MapToControlOutput(unit.exit);
            
            ListHelpersVS.GetListCount(list, forLoop.ValueIn(Flow_ForLoopNode.IdEndIndex));
            
            ListHelpersVS.GetItem(unitExporter, list, forLoop.ValueOut(Flow_ForLoopNode.IdIndex), out var getItemValueSocket);

            forLoop.ValueOut(Flow_ForLoopNode.IdIndex).MapToPort(unit.currentIndex);
            
            getItemValueSocket.MapToPort(unit.currentItem);
            return true;
        }
    }
}