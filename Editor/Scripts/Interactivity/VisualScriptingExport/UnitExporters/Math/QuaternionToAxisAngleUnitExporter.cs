using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionToAxisAngleUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.ToAngleAxis), new QuaternionToAxisAngleUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var node = unitExporter.CreateNode<Math_QuatToAxisAngleNode>();
            node.ValueIn(Math_QuatToAxisAngleNode.IdValueA).MapToInputPort(unit.valueInputs[0]);
            node.ValueOut(Math_QuatToAxisAngleNode.IdOutAxis).MapToPort(unit.valueOutputs["&axis"]);
            
            var degToRadNode = unitExporter.CreateNode<Math_RadNode>();
            degToRadNode.ValueIn(Math_RadNode.IdInputA).ConnectToSource(node.ValueOut(Math_QuatToAxisAngleNode.IdOutAngle));
            degToRadNode.FirstValueOut().MapToPort(unit.valueOutputs["&angle"]);

            return true;
        }
    }
}