using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class QuaternionEulerUnitExport : IUnitExporter
    {
        public Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Quaternion), nameof(Quaternion.Euler), new QuaternionEulerUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var invokeMember = unitExporter.unit as InvokeMember;

            GltfInteractivityUnitExporterNode.ValueOutputSocketData result;
            if (invokeMember.valueInputs.Count == 1)
            {
                // Vector3 Input
                QuaternionHelpers.CreateQuaternionFromEuler(unitExporter, invokeMember.valueInputs[0], out result);
            }
            else
            {
                // XYZ Input
                QuaternionHelpers.CreateQuaternionFromEuler(unitExporter, 
                    invokeMember.valueInputs[0],
                    invokeMember.valueInputs[1],
                    invokeMember.valueInputs[2],
                    out result);
            }

            result.MapToPort(invokeMember.result);
            unitExporter.ByPassFlow(invokeMember.enter, invokeMember.exit);
            return true;
        }
    }
}