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
           
           TransformHelpersVS.GetLocalPosition(unitExporter, unit.target, out var positionOutput);
           var add = unitExporter.CreateNode<Math_AddNode>();
           add.ValueIn(Math_AddNode.IdValueB).ConnectToSource(positionOutput);
           
           if (unit.valueInputs.Skip(1).First().type == typeof(Vector3))
           {
               // translate value is a vector3
               add.ValueIn(Math_AddNode.IdValueA).MapToInputPort(unit.valueInputs.Skip(1).First());
           }
           else
           {
               // translate value is separate floats
               var combine3 = unitExporter.CreateNode<Math_Combine3Node>();

                combine3.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(unit.valueInputs[1]);
                combine3.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(unit.valueInputs[2]);
                combine3.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(unit.valueInputs[3]);
              
                add.ValueIn(Math_AddNode.IdValueA).ConnectToSource(combine3.FirstValueOut());
           }
           //TODO: translate of non self

           TransformHelpersVS.SetLocalPosition(unitExporter, unit.target,  add.FirstValueOut(), unit.enter, unit.exit);
           return true;
        }
    }
}