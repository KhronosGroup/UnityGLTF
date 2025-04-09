using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_SetPositionUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.position), new Transform_SetPositionUnitExport(true));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localPosition), new Transform_SetPositionUnitExport(false));
        }
        
        public Transform_SetPositionUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.SetMember;
           if (worldSpace)
                TransformHelpersVS.SetWorldPosition(unitExporter, unit.target, unit.input, unit.assign, unit.assigned);
           else
                TransformHelpersVS.SetLocalPosition(unitExporter, unit.target, unit.input, unit.assign, unit.assigned);
           
           return true;
        }
    }
}