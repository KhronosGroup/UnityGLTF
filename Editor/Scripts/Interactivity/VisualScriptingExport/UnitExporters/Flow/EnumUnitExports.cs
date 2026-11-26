using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Switch On Enum as flow/switch on the underlying int values of the enum.
    /// </summary>
    public class SwitchOnEnumUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SwitchOnEnum); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SwitchOnEnumUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SwitchOnEnum;
            if (unit.branches == null || unit.branches.Count == 0)
            {
                UnitExportLogging.AddErrorLog(unit, "No enum type selected.");
                return false;
            }

            var node = unitExporter.CreateNode<Flow_SwitchNode>();
            unitExporter.MapInputPortToSocketName(unit.enter, Flow_SwitchNode.IdFlowIn, node);

            node.Configuration[Flow_SwitchNode.IdConfigurationCases] = new GltfInteractivityNode.ConfigData
            {
                Value = unit.branches.Keys.Select(k => Convert.ToInt32(k)).ToArray()
            };

            foreach (var branch in unit.branches)
                node.FlowOut(Convert.ToInt32(branch.Key).ToString()).MapToControlOutput(branch.Value);

            node.ValueIn(Flow_SwitchNode.IdSelection).MapToInputPort(unit.@enum).SetType(TypeRestriction.LimitToInt);
            return true;
        }
    }

    /// <summary>
    /// Select On Enum as math/switch on the underlying int values of the enum.
    /// </summary>
    public class SelectOnEnumUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SelectOnEnum); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SelectOnEnumUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SelectOnEnum;
            if (unit.branches == null || unit.branches.Count == 0)
            {
                UnitExportLogging.AddErrorLog(unit, "No enum type selected.");
                return false;
            }

            var switchNode = unitExporter.CreateNode<Math_SwitchNode>();
            switchNode.Configuration[Math_SwitchNode.IdConfigCases].Value = unit.branches.Keys.Select(k => Convert.ToInt32(k)).ToArray();
            switchNode.ValueIn(Math_SwitchNode.IdSelection).MapToInputPort(unit.selector).SetType(TypeRestriction.LimitToInt);

            // Select On Enum has no default, every enum value has a branch. Use the first one for values outside of the enum.
            switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).MapToInputPort(unit.branches.First().Value);

            var valueOut = switchNode.ValueOut(Math_SwitchNode.IdOut).MapToPort(unit.selection);
            foreach (var branch in unit.branches)
            {
                var inPort = switchNode.ValueIn(Convert.ToInt32(branch.Key).ToString()).MapToInputPort(branch.Value);
                valueOut.ExpectedType(ExpectedType.FromInputSocket(inPort.socket.Key));
            }

            return true;
        }
    }
}
