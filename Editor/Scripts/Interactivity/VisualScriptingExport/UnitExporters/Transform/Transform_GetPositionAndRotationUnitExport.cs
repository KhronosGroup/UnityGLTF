using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetPositionAndRotationUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.GetPositionAndRotation), new Transform_GetPositionAndRotationUnitExport(true));
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.GetLocalPositionAndRotation), new Transform_GetPositionAndRotationUnitExport(false));
        }
        
        public Transform_GetPositionAndRotationUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;
           
           if (worldSpace)
           {
                TransformHelpers.GetWorldPosition(unitExporter, out var posTargetRef, out var posRef);
                TransformHelpers.GetWorldRotation(unitExporter, out var rotTargetRef, out var rotRef);
                posTargetRef.MapToInputPort(unit.target);
                rotTargetRef.MapToInputPort(unit.target);
                
                posRef.MapToPort(unit.valueOutputs["%position"]);
                rotRef.MapToPort(unit.valueOutputs["%rotation"]);
                
           }
           else
           {
               TransformHelpers.GetLocalPosition(unitExporter, out var posTargetRef, out var posRef);
               TransformHelpers.GetLocalRotation(unitExporter, out var rotTargetRef, out var rotRef);
               posTargetRef.MapToInputPort(unit.target);
               rotTargetRef.MapToInputPort(unit.target);
                
               posRef.MapToPort(unit.valueOutputs["%localPosition"]);
               rotRef.MapToPort(unit.valueOutputs["%localRotation"]);
                
           }
           
           unitExporter.ByPassFlow(unit.enter, unit.exit);
           
           return true;
        }
    }
}