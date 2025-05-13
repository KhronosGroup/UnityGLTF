using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnStartUnitExport : IUnitExporter
    {
        public System.Type unitType { get => typeof(Start); }
         
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnStartUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Start;
            var node = unitExporter.CreateNode<Event_OnStartNode>();
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnStartNode.IdFlowOut, node);
            return true;
        }
    }
}