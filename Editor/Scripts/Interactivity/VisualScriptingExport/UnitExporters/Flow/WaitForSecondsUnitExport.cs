using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WaitForSecondsUnitExport : IUnitExporter, ICoroutineWait
    {
        public System.Type unitType { get => typeof( WaitForSecondsUnit); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WaitForSecondsUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WaitForSecondsUnit;
            var node = unitExporter.CreateNode<Flow_SetDelayNode>();
            
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Flow_SetDelayNode.IdFlowDone, node);
            
            unitExporter.MapInputPortToSocketName(unit.seconds, Flow_SetDelayNode.IdDuration, node);
            unitExporter.MapInputPortToSocketName(unit.enter, Flow_SetDelayNode.IdFlowIn, node);
            // TODO: cancel, err, lastDelayIndex ... maybe custom Unit also with a Static Dict. for delay index
           
            unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
            {
                var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, node as GltfInteractivityUnitExporterNode);
                if (awaiter == null)
                    return;
                
                awaiter.AddCoroutineWait(unitExporter, node, Flow_SetDelayNode.IdFlowDone);
            };
            return true;
        }
    }
}