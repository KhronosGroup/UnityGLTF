using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
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
            var node = unitExporter.CreateNode<Event_OnTickNode>();
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnTickNode.IdFlowOut, node);
            return true;
        }

    }

    /// <summary>
    /// glTF has only one tick event, so LateUpdate is exported like Update.
    /// </summary>
    public class OnLateUpdateUnitExport : IUnitExporter
    {
        public System.Type unitType { get => typeof(LateUpdate); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnLateUpdateUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as LateUpdate;
            var node = unitExporter.CreateNode<Event_OnTickNode>();
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnTickNode.IdFlowOut, node);
            return true;
        }
    }

    /// <summary>
    /// glTF has no fixed time step, so FixedUpdate is exported as a tick event (once per frame instead of at a fixed rate).
    /// </summary>
    public class OnFixedUpdateUnitExport : IUnitExporter
    {
        public System.Type unitType { get => typeof(FixedUpdate); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnFixedUpdateUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as FixedUpdate;
            var node = unitExporter.CreateNode<Event_OnTickNode>();
            unitExporter.MapOutFlowConnectionWhenValid(unit.trigger, Event_OnTickNode.IdFlowOut, node);
            UnitExportLogging.AddWarningLog(unit, "FixedUpdate is exported as a tick event: it runs once per frame, not at a fixed rate.");
            return true;
        }
    }
}