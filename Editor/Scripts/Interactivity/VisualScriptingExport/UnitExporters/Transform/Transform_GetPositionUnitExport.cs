using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetPositionUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.position), new Transform_GetPositionUnitExport(true));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localPosition), new Transform_GetPositionUnitExport(false));
        }
        
        public Transform_GetPositionUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.GetMember;
           if (worldSpace)
               TransformHelpersVS.GetWorldPosition(unitExporter, unit.target, unit.value);
           else
               TransformHelpersVS.GetLocalPosition(unitExporter, unit.target, unit.value);
           return true;
        }
    }
}