using System;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetRotationUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.rotation), new Transform_GetRotationUnitExport(true));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localRotation), new Transform_GetRotationUnitExport(false));
        }
        
        public Transform_GetRotationUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.GetMember;
           if (worldSpace)
                TransformHelpersVS.GetWorldRotation(unitExporter, unit.target, unit.value);
           else
                TransformHelpersVS.GetLocalRotation(unitExporter, unit.target, unit.value);
           return true;
        }
    }
}