using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetDirectionVectorsUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        private Vector3 direction = Vector3.forward;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.forward), new Transform_GetDirectionVectorsUnitExport(Vector3.forward));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.up), new Transform_GetDirectionVectorsUnitExport(Vector3.up));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.right), new Transform_GetDirectionVectorsUnitExport(Vector3.right));
        }

        public Transform_GetDirectionVectorsUnitExport(Vector3 dir)
        {
            direction = dir;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMember = unitExporter.unit as Unity.VisualScripting.GetMember;
    
            TransformHelpers.GetWorldRotation(unitExporter, out var targetRef, out var rotationSocket);
            targetRef.MapToInputPort(getMember.target);
            
            var rotate = unitExporter.CreateNode<Math_Rotate3dNode>();
            rotate.ValueIn(Math_Rotate3dNode.IdInputQuaternion).ConnectToSource(rotationSocket);
            rotate.ValueIn(Math_Rotate3dNode.IdInputVector).SetValue(direction);

            rotate.FirstValueOut().MapToPort(getMember.value);
            
            return true;
        }
    }
}