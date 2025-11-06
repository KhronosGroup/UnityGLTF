using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Mathf_CeilToIntUnitExport : IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.CeilToInt),
                new Mathf_CeilToIntUnitExport());
        }

        public Type unitType { get => typeof(InvokeMember); }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var ceilNode = unitExporter.CreateNode<Math_CeilNode>();

            var floatToIntNode = unitExporter.CreateNode<Type_FloatToIntNode>();

            ceilNode.ValueIn(Math_CeilNode.IdValueA).MapToInputPort(unit.valueInputs[0]);

            floatToIntNode.ValueIn(Type_FloatToIntNode.IdInputA)
                .ConnectToSource(ceilNode.ValueOut(Math_CeilNode.IdOut));
            
            floatToIntNode.ValueOut(Math_CeilNode.IdOut).MapToPort(unit.result);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
         }
        
    }
}