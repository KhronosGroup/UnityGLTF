using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public class OnPointerClickEventExport : GameObjectEvents<OnPointerClick, Event_OnSelectNode>
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            //NodeConvertRegistry.RegisterImport(new OnSelectNode());
            UnitExporterRegistry.RegisterExporter(new OnPointerClickEventExport());
        }
        
        protected override void OnTargetNodeConfigured(UnitExporter unitExporter, int nodeIndex)
        {
            unitExporter.exportContext.AddSelectabilityExtensionToNode(nodeIndex);
        }
    }
    
}