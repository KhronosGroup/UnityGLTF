using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WaitUntilUnitExport : IUnitExporter
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
            return true;
        }
    }
}