using System;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class IfUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(Unity.VisualScripting.If);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new IfUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.If;
            var node = unitExporter.CreateNode<Flow_BranchNode>();
            
            unitExporter.MapInputPortToSocketName(unit.enter, Flow_BranchNode.IdFlowIn, node);
            unitExporter.MapInputPortToSocketName(unit.condition, Flow_BranchNode.IdCondition, node);
            unitExporter.MapOutFlowConnectionWhenValid(unit.ifTrue, Flow_BranchNode.IdFlowOutTrue, node);
            unitExporter.MapOutFlowConnectionWhenValid(unit.ifFalse, Flow_BranchNode.IdFlowOutFalse, node);
            return true;
        }
    }
}