using System;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_EulerAngles : IUnitExporter
    {
        public Type unitType { get; }

        private bool worldSpace = false;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.eulerAngles), new Transform_EulerAngles(true));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localEulerAngles), new Transform_EulerAngles(false));
        }
        
        public Transform_EulerAngles(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            // TODO: World Space conversion
            
            var unit = unitExporter.unit as Unity.VisualScripting.SetMember;
            QuaternionHelpers.CreateQuaternionFromEuler(unitExporter, unit.input, out var quaternion);
            
            TransformHelpers.SetLocalRotation(unitExporter, unit.target, quaternion, unit.assign, unit.assigned);
            return true;
        }
    }
}