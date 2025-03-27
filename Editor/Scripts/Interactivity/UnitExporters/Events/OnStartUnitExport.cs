using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
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
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Event_OnStartNode());
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnStartNode.IdFlowOut, node);
            return true;
        }
    }
}