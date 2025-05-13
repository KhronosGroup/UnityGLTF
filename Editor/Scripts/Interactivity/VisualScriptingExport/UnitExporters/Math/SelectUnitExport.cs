using System;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SelectUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(Unity.VisualScripting.SelectUnit);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SelectUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.SelectUnit;
            var node = unitExporter.CreateNode<Math_SelectNode>();
            
            unitExporter.MapInputPortToSocketName(unit.condition, Math_SelectNode.IdCondition, node);
            unitExporter.MapInputPortToSocketName(unit.ifTrue, Math_SelectNode.IdValueA, node);
            unitExporter.MapInputPortToSocketName(unit.ifFalse, Math_SelectNode.IdValueB, node);
            unitExporter.MapValueOutportToSocketName(unit.selection, Math_SelectNode.IdOutValue, node);
            return true;
        }
    }
}