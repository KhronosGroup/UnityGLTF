using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SwitchOnIntegerUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SwitchOnInteger); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SwitchOnIntegerUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SwitchOnInteger;
            var node = unitExporter.CreateNode<Flow_SwitchNode>();
            
            unitExporter.MapInputPortToSocketName(unit.enter, Flow_SwitchNode.IdFlowIn, node);

            node.Configuration["cases"] = new GltfInteractivityNode.ConfigData
            {
                Value = unit.branches.Select(b => b.Key).ToArray()
            };
            
            foreach (var branch in unit.branches)
                node.FlowOut(branch.Key.ToString()).MapToControlOutput(branch.Value);

            node.FlowOut(Flow_SwitchNode.IdFDefaultFlowOut).MapToControlOutput(unit.@default);
            node.ValueIn(Flow_SwitchNode.IdSelection).MapToInputPort(unit.selector);
            return true;
        }
    }
}