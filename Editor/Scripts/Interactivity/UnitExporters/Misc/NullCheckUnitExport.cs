using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class NullCheckUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(NullCheck); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new NullCheckUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as NullCheck;

            var eqNode = unitExporter.CreateNode(new Math_EqNode());
            eqNode.ValueIn("a").MapToInputPort(unit.input).SetType(TypeRestriction.LimitToInt);
            eqNode.ValueIn("b").SetValue(-1).SetType(TypeRestriction.LimitToInt);
            eqNode.FirstValueOut().ExpectedType(ExpectedType.Bool);
            
            var branch = unitExporter.CreateNode(new Flow_BranchNode());
            branch.ValueIn(Flow_BranchNode.IdCondition).ConnectToSource(eqNode.FirstValueOut());
            branch.FlowIn(Flow_BranchNode.IdFlowIn).MapToControlInput(unit.enter);
            branch.FlowOut(Flow_BranchNode.IdFlowOutTrue).MapToControlOutput(unit.ifNull);
            branch.FlowOut(Flow_BranchNode.IdFlowOutFalse).MapToControlOutput(unit.ifNotNull);
            return true;
        }
    }
}