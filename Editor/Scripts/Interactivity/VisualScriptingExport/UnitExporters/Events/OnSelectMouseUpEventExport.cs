using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public class OnSelectMouseUpEventExport : GameObjectEvents<OnMouseUp, Event_OnSelectNode>
    {
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnSelectMouseUpEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.exportContext.AddSelectabilityExtensionToNode(nodeIndex);
        }
    }
}