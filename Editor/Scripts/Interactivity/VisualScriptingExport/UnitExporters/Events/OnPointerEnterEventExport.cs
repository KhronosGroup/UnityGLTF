using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnPointerEnterEventExport : GameObjectEvents<OnPointerEnter, Event_OnHoverInNode>
    {
        protected override string NodeIndexId
        {
            get => Event_OnHoverInNode.IdOutHoverNodeIndex;
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnPointerEnterEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }
}