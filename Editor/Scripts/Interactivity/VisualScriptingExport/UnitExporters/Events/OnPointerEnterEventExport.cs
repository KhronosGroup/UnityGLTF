using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnPointerEnterEventExport : GameObjectEvents<OnPointerEnter, Event_OnHoverInNode>
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnPointerEnterEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.exportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }
}