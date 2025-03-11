using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
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