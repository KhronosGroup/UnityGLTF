using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    // glTF has no separate press/release events, only event/onSelect. Up and click events are exported as onSelect,
    // down events are not exported, otherwise a graph with down and up events would trigger both on one select.

    public class OnMouseUpAsButtonEventExport : GameObjectEvents<OnMouseUpAsButton, Event_OnSelectNode>
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnMouseUpAsButtonEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddSelectabilityExtensionToNode(nodeIndex);
        }
    }

    public class OnPointerUpEventExport : GameObjectEvents<OnPointerUp, Event_OnSelectNode>
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnPointerUpEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddSelectabilityExtensionToNode(nodeIndex);
        }
    }

    public class OnMouseEnterEventExport : GameObjectEvents<OnMouseEnter, Event_OnHoverInNode>
    {
        protected override string NodeRefId
        {
            get => Event_OnHoverInNode.IdOutHoverNodeRef;
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnMouseEnterEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }

    public class OnMouseExitEventExport : GameObjectEvents<OnMouseExit, Event_OnHoverOutNode>
    {
        protected override string NodeRefId
        {
            get => Event_OnHoverOutNode.IdOutHoverNodeRef;
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnMouseExitEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }
}
