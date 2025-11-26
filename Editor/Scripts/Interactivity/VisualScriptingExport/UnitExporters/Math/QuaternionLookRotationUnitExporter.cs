using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionLookRotationUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.LookRotation), new QuaternionLookRotationUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            var node = unitExporter.CreateNode<Math_QuatFromUpForwardNode>();
            node.ValueIn(Math_QuatFromUpForwardNode.IdForward).MapToInputPort(unit.valueInputs[0]);
            if (unit.valueInputs.Count > 1)
                node.ValueIn(Math_QuatFromUpForwardNode.IdUp).MapToInputPort(unit.valueInputs[1]);
            else
                node.ValueIn(Math_QuatFromUpForwardNode.IdUp).SetValue(Vector3.up);
            node.ValueOut(Math_QuatFromUpForwardNode.IdOutValue).MapToPort(unit.result);

            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}