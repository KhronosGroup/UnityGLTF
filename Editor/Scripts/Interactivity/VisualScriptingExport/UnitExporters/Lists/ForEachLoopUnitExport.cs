using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Interactivity.Schema;

namespace Editor.UnitExporters.Lists
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
            
            var list = ListHelpers.FindListByConnections(unitExporter.exportContext, unit);
            if (list == null)
            {
                Debug.LogError("Can't resolve list input for ForEachLoop");
            }
            var forLoop = unitExporter.CreateNode(new Flow_ForLoopNode());
            forLoop.ConfigurationData[Flow_ForLoopNode.IdConfigInitialIndex].Value = 0;
            forLoop.ValueIn(Flow_ForLoopNode.IdStartIndex).SetValue(0);

            forLoop.FlowIn(Flow_ForLoopNode.IdFlowIn).MapToControlInput(unit.enter);
            forLoop.FlowOut(Flow_ForLoopNode.IdLoopBody).MapToControlOutput(unit.body);
            forLoop.FlowOut(Flow_ForLoopNode.IdCompleted).MapToControlOutput(unit.exit);
            
            ListHelpers.GetListCount(list, forLoop.ValueIn(Flow_ForLoopNode.IdEndIndex));
            
            ListHelpers.GetItem(unitExporter, list, forLoop.ValueOut(Flow_ForLoopNode.IdIndex), out var getItemValueSocket);

            forLoop.ValueOut(Flow_ForLoopNode.IdIndex).MapToPort(unit.currentIndex);
            
            getItemValueSocket.MapToPort(unit.currentItem);
            return true;
        }
    }
}