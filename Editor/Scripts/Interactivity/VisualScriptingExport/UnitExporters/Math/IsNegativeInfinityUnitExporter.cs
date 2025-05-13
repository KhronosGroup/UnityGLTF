using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class IsNegativeInfinityUnitExporter : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(InvokeUnitExport);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(float), nameof(float.IsNegativeInfinity), new IsNegativeInfinityUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            if (unit.valueInputs.Count == 0)
                return false;
            
            var node = unitExporter.CreateNode<Math_IsInfNode>();
            node.ValueIn("a").MapToInputPort(unit.valueInputs[0]);
            
            var lt = unitExporter.CreateNode<Math_EqNode>();
            lt.ValueIn("a").MapToInputPort(unit.valueInputs[0]).SetType(TypeRestriction.LimitToFloat);
            lt.ValueIn("b").SetValue(0f);
            
            var and = unitExporter.CreateNode<Math_AddNode>();
            and.ValueIn("a").ConnectToSource(node.ValueOut("value"));
            and.ValueIn("b").ConnectToSource(lt.ValueOut("value"));
            and.ValueOut("value").MapToPort(unit.result);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}