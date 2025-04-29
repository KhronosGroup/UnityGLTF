using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class OnPointerExitEventExport : GameObjectEvents<OnPointerExit, Event_OnHoverOutNode>
    {
        protected override string NodeIndexId
        {
            get => Event_OnHoverOutNode.IdOutHoverNodeIndex;
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new OnPointerExitEventExport());
        }

        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.vsExportContext.AddHoverabilityExtensionToNode(nodeIndex);
        }
    }
}