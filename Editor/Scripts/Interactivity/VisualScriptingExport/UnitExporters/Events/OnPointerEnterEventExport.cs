using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
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