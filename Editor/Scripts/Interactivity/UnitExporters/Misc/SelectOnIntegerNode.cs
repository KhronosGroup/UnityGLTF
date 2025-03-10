using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class SelectOnIntegerNode : IUnitExporter
    {
        public System.Type unitType => typeof(Unity.VisualScripting.SelectOnInteger);

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SelectOnIntegerNode());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SelectOnInteger;
            
            var switchNode = unitExporter.CreateNode(new Math_SwitchNode());
            int[] cases = new int[unit.branches.Count];

            for (int i = 0; i < unit.branches.Count; i++)
                cases[i] = (int)unit.branches[i].Key;

            switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).MapToInputPort(unit.@default);

            switchNode.ValueIn(Math_SwitchNode.IdSelection).MapToInputPort(unit.selector);
            var valueout = switchNode.ValueOut(Math_SwitchNode.IdOut).MapToPort(unit.selection);
            
            switchNode.ConfigurationData[Math_SwitchNode.IdConfigCases].Value = cases;
            for (int i = 0; i < unit.branches.Count; i++)
            {
                var branch = unit.branches[i];
                var inPort = switchNode.ValueIn(branch.Key.ToString()).MapToInputPort(branch.Value);
                valueout.ExpectedType(ExpectedType.FromInputSocket(inPort.socket.Key));
            }
            
            return true;
        }
    }
}
