using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WaitForFlowUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(WaitForFlow);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WaitForFlowUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WaitForFlow;
            var node = unitExporter.CreateNode<Flow_WaitAllNode>();
            
            node.Configuration[Flow_WaitAllNode.IdConfigInputFlows].Value = unit.inputCount;

            unitExporter.MapInputPortToSocketName(unit.reset, Flow_WaitAllNode.IdFlowInReset, node);
            
            for (int i = 0; i < unit.inputCount; i++)
            {
                unitExporter.MapInputPortToSocketName(unit.awaitedInputs[i], i.ToString(), node);
            }
            
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_WaitAllNode.IdFlowOutCompleted, node);
            return true;
        }
    }
}