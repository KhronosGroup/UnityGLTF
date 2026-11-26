using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_TransformVectorUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }

        private bool inverse;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.TransformVector), new Transform_TransformVectorUnitExport(false));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.InverseTransformVector), new Transform_TransformVectorUnitExport(true));
        }

        public Transform_TransformVectorUnitExport(bool inverse)
        {
            this.inverse = inverse;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var invokeUnit = unitExporter.unit as InvokeMember;

            // TransformVector is affected by scale and rotation, but not by position
            ValueInRef vectorSocket;
            ValueOutRef resultSocket;
            if (inverse)
                TransformHelpersVS.GetLocalVectorFromWorldVector(unitExporter, invokeUnit.target, out vectorSocket, out resultSocket);
            else
                TransformHelpersVS.GetWorldVectorFromLocalVector(unitExporter, invokeUnit.target, out vectorSocket, out resultSocket);

            if (invokeUnit.valueInputs.Count > 2)
            {
                var combine = unitExporter.CreateNode<Math_Combine3Node>();
                combine.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(invokeUnit.valueInputs["%x"]);
                combine.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(invokeUnit.valueInputs["%y"]);
                combine.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(invokeUnit.valueInputs["%z"]);
                vectorSocket.ConnectToSource(combine.FirstValueOut());
            }
            else
                vectorSocket.MapToInputPort(invokeUnit.valueInputs["%vector"]);

            resultSocket.MapToPort(invokeUnit.result);

            unitExporter.ByPassFlow(invokeUnit.enter, invokeUnit.exit);

            return true;
        }
    }
}
