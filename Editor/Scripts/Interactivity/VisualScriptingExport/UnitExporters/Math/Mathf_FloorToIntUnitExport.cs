using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Mathf_FloorToIntUnitExport : IUnitExporter
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Mathf), nameof(Mathf.FloorToInt),
                new Mathf_FloorToIntUnitExport());
        }

        public Type unitType { get => typeof(InvokeMember); }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var floorNode = unitExporter.CreateNode<Math_FloorNode>();
            
            var floatToIntNode = unitExporter.CreateNode<Type_FloatToIntNode>();
   
            floorNode.ValueIn(Math_FloorNode.IdInputA).MapToInputPort(unit.valueInputs[0]);
            floatToIntNode.ValueIn(Type_FloatToIntNode.IdInputA).ConnectToSource(floorNode.ValueOut(Math_FloorNode.IdValueResult));

            unitExporter.MapValueOutportToSocketName(unit.result, Type_FloatToIntNode.IdValueResult, floatToIntNode);
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
         }
        
    }
}