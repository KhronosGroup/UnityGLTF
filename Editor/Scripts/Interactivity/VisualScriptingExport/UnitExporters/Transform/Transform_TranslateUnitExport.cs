using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_TranslateUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.Translate), new Transform_TranslateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;
           
           TransformHelpers.GetLocalPosition(unitExporter, unit.target, out var positionOutput);
           var add = unitExporter.CreateNode(new Math_AddNode());
           add.ValueIn(Math_AddNode.IdValueB).ConnectToSource(positionOutput);
           
           if (unit.valueInputs.Skip(1).First().type == typeof(Vector3))
           {
               // translate value is a vector3
               unitExporter.MapInputPortToSocketName(unit.valueInputs.Skip(1).First(), Math_AddNode.IdValueA, add);
           }
           else
           {
               // translate value is separate floats
               var combine3 = unitExporter.CreateNode(new Math_Combine3Node());

               unitExporter.MapInputPortToSocketName(unit.valueInputs[1], "a", combine3);
               unitExporter.MapInputPortToSocketName(unit.valueInputs[2], "b", combine3);
               unitExporter.MapInputPortToSocketName(unit.valueInputs[3], "c", combine3);
               
               unitExporter.MapInputPortToSocketName("value", combine3, Math_AddNode.IdValueA, add);
           }
           //TODO: translate of non self

           TransformHelpers.SetLocalPosition(unitExporter, unit.target,  add.FirstValueOut(), unit.enter, unit.exit);
           return true;
        }
    }
}