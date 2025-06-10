using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionFromToDirectionUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.FromToRotation), new QuaternionFromToDirectionUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var node = unitExporter.CreateNode<Math_QuatFromDirectionsNode>();
            node.ValueIn(Math_QuatFromAxisAngleNode.IdAngle).MapToInputPort(unit.valueInputs[0]);
            node.ValueIn(Math_QuatFromAxisAngleNode.IdAxis).MapToInputPort(unit.valueInputs[1]);
            node.ValueOut(Math_QuatFromAxisAngleNode.IdOutValue).MapToPort(unit.result);

            return true;
        }
    }
}