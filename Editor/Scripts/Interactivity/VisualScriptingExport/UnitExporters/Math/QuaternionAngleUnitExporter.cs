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
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Angle), new QuaternionAngleUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            // Not math/quatAngleBetween: Unity uses |dot|, so the result is the shortest angle in [0, 180] degrees
            UnityMathExpressions.QuaternionAngle(unitExporter, unit.valueInputs[0], unit.valueInputs[1])
                .MapToPort(unit.result);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}