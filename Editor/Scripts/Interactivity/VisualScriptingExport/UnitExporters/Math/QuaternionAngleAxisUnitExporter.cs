using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionAngleAxisUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.AngleAxis), new QuaternionAngleAxisUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var node = unitExporter.CreateNode<Math_QuatFromAxisAngleNode>();
            node.ValueIn(Math_QuatFromAxisAngleNode.IdAxis).MapToInputPort(unit.valueInputs[1]);
            node.ValueOut(Math_QuatFromAxisAngleNode.IdOutValue).MapToPort(unit.result);
            
            var degToRadNode = unitExporter.CreateNode<Math_RadNode>();
            degToRadNode.ValueIn(Math_RadNode.IdInputA).MapToInputPort(unit.valueInputs[0]);
            node.ValueIn(Math_QuatFromAxisAngleNode.IdAngle).ConnectToSource(degToRadNode.FirstValueOut());
            
            return true;
        }
    }
}