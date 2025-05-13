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

            ceilNode.ValueIn(Math_CeilNode.IdInputA).MapToInputPort(unit.valueInputs[0]);

            floatToIntNode.ValueIn(Type_FloatToIntNode.IdInputA)
                .ConnectToSource(ceilNode.ValueOut(Math_CeilNode.IdValueResult));
            
            floatToIntNode.ValueOut(Math_CeilNode.IdValueResult).MapToPort(unit.result);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
         }
        
    }
}