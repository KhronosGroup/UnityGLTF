using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnPointerExitEventExport : GameObjectEvents<OnPointerExit, Event_OnHoverOutNode>
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnPointerExitEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.exportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }
}