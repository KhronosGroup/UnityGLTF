using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_SetPositionAndRotationUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.SetPositionAndRotation), new Transform_SetPositionAndRotationUnitExport(true));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.SetLocalPositionAndRotation), new Transform_SetPositionAndRotationUnitExport(false));
        }
        
        public Transform_SetPositionAndRotationUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;

           if (worldSpace)
           {
                TransformHelpers.SetWorldPosition(unitExporter, out var posTargetRef, out var posRef, out var posFlowIn, out var posFlowOut);
                TransformHelpers.SetWorldRotation(unitExporter, out var rotTargetRef, out var rotRef, out var rotFlowIn, out var rotFlowOut);
                posTargetRef.MapToInputPort(unit.target);
                rotTargetRef.MapToInputPort(unit.target);
                
                posRef.MapToInputPort(unit.valueInputs["%position"]);
                rotRef.MapToInputPort(unit.valueInputs["%rotation"]);
                
                posFlowIn.MapToControlInput(unit.enter);
                posFlowOut.ConnectToFlowDestination(rotFlowIn);
                rotFlowOut.MapToControlOutput(unit.exit);
           }
           else
           {
               TransformHelpers.SetLocalPosition(unitExporter, out var posTargetRef, out var posRef, out var posFlowIn, out var posFlowOut);
               TransformHelpers.SetLocalRotation(unitExporter, out var rotTargetRef, out var rotRef, out var rotFlowIn, out var rotFlowOut);
               posTargetRef.MapToInputPort(unit.target);
               rotTargetRef.MapToInputPort(unit.target);
                
               posRef.MapToInputPort(unit.valueInputs["%localPosition"]);
               rotRef.MapToInputPort(unit.valueInputs["%localRotation"]);
                
               posFlowIn.MapToControlInput(unit.enter);
               posFlowOut.ConnectToFlowDestination(rotFlowIn);
               rotFlowOut.MapToControlOutput(unit.exit);
           }
           
           return true;
        }
    }
}