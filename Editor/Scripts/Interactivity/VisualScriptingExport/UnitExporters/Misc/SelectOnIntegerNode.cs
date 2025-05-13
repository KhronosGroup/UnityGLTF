using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
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
            
            var switchNode = unitExporter.CreateNode<Math_SwitchNode>();
            int[] cases = new int[unit.branches.Count];

            for (int i = 0; i < unit.branches.Count; i++)
                cases[i] = (int)unit.branches[i].Key;

            if (unit.@default.hasValidConnection || unit.@default.hasDefaultValue)
                switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).MapToInputPort(unit.@default);
            else
            {
                if (unit.branches.Count > 0)
                {
                    var firstBranch = unit.branches[0];
                    switchNode.ValueIn(Math_SwitchNode.IdDefaultValue).MapToInputPort(firstBranch.Value);
                }
     
            }
                
            if (unit.branches.Count == 0)
            {
                UnitExportLogging.AddErrorLog(unit, "There are no branches in the SelectOnInteger node.");
                return false;
            }
            
            switchNode.ValueIn(Math_SwitchNode.IdSelection).MapToInputPort(unit.selector);
            var valueout = switchNode.ValueOut(Math_SwitchNode.IdOut).MapToPort(unit.selection);
            
            switchNode.Configuration[Math_SwitchNode.IdConfigCases].Value = cases;
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
