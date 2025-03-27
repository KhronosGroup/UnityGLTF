using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class OnUpdateUnitExport : IUnitExporter
    {
        public System.Type unitType { get => typeof(Update); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnUpdateUnitExport());
        }
        
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Update;
            GltfInteractivityUnitExporterNode node = unitExporter.CreateNode(new Event_OnTickNode());
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnTickNode.IdFlowOut, node);
            return true;
        }

    }
}