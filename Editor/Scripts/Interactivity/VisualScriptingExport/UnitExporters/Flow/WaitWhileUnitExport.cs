using System;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class WaitWhileUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(WaitWhileUnit); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new WaitWhileUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as WaitWhileUnit;

            FlowHelpersVS.CreateConditionalWaiting(unitExporter,
                out var condition,
                out var flowIn,
                false,
                out var flowWhenDone);

            condition.MapToInputPort(unit.condition);
            flowIn.MapToControlInput(unit.enter);
            flowWhenDone.MapToControlOutput(unit.exit);
            
            return true;
        }
    }
}