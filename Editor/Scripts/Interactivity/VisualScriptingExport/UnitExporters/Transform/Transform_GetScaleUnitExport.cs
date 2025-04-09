using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetScaleUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.lossyScale), new Transform_GetScaleUnitExport(true));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localScale), new Transform_GetScaleUnitExport(false));
        }
        
        public Transform_GetScaleUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.GetMember;
           
           if (worldSpace)
               TransformHelpersVS.GetWorldScale(unitExporter, unit.target, unit.value);
           else
               TransformHelpersVS.GetLocalScale(unitExporter, unit.target, unit.value);
           
           return true;
        }
    }
}