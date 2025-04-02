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
            
            var ceilNode = unitExporter.CreateNode(new Math_CeilNode());

            var floatToIntNode = unitExporter.CreateNode(new Type_FloatToIntNode());

            unitExporter.MapInputPortToSocketName(unit.valueInputs[0], Math_FloorNode.IdInputA, ceilNode);
            unitExporter.MapInputPortToSocketName(Math_FloorNode.IdValueResult, ceilNode, Type_FloatToIntNode.IdInputA, floatToIntNode);

            unitExporter.MapValueOutportToSocketName(unit.result, Type_FloatToIntNode.IdValueResult, floatToIntNode);
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
         }
        
    }
}