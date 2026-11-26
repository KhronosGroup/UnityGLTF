using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WaitUntilUnitExport : IUnitExporter, ICoroutineWait
    {
        public Type unitType { get => typeof(WaitUntilUnit); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WaitUntilUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WaitUntilUnit;

            FlowHelpersVS.CreateConditionalWaiting(unitExporter,
                out var condition,
                out var flowIn,
                true,
                out var flowWhenDone);

            condition.MapToInputPort(unit.condition);
            flowIn.MapToControlInput(unit.enter);
            flowWhenDone.MapToControlOutput(unit.exit);

            // an enclosing Sequence or loop waits until the condition was met
            unitExporter.vsExportContext.OnUnitNodesCreated += (nodes) =>
            {
                var awaiter = CoroutineHelper.FindCoroutineAwaiter(unitExporter, flowIn.node as GltfInteractivityUnitExporterNode);
                if (awaiter == null)
                    return;
                awaiter.AddCoroutineWait(unitExporter, flowIn.node, flowIn.socket.Key, flowWhenDone.node, flowWhenDone.socket.Key);
            };
            return true;
        }
    }
}