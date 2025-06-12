using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionAngleUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Angle), new QuaternionToAxisAngleUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var node = unitExporter.CreateNode<Math_QuatAngleBetweenNode>();
            node.ValueIn(Math_QuatToAxisAngleNode.IdValueA).MapToInputPort(unit.valueInputs[0]);
            node.ValueIn(Math_QuatToAxisAngleNode.IdValueA).MapToInputPort(unit.valueInputs[1]);

            var degToRadNode = unitExporter.CreateNode<Math_RadNode>();
            degToRadNode.ValueIn(Math_RadNode.IdInputA).ConnectToSource(node.FirstValueOut());
            degToRadNode.FirstValueOut().MapToPort(unit.result);
            
            return true;
        }
    }
}