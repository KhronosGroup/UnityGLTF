using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_SetDirectionVectorsUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        private Vector3 direction = Vector3.forward;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.forward), new Transform_SetDirectionVectorsUnitExport(Vector3.forward));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.up), new Transform_SetDirectionVectorsUnitExport(Vector3.up));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.right), new Transform_SetDirectionVectorsUnitExport(Vector3.right));
        }

        public Transform_SetDirectionVectorsUnitExport(Vector3 dir)
        {
            direction = dir;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMember = unitExporter.unit as Unity.VisualScripting.SetMember;

            var r = unitExporter.CreateNode<Math_QuatFromDirectionsNode>();
            r.ValueIn(Math_QuatFromDirectionsNode.IdValueA).SetValue(direction);
            r.ValueIn(Math_QuatFromDirectionsNode.IdValueB).MapToInputPort(getMember.input);
            
            TransformHelpers.SetWorldRotation(unitExporter, out var targetRef, out var setRotationRef, out var setRotationFlowIn, out var setRotationFlowOut);
            targetRef.MapToInputPort(getMember.target);
            setRotationRef.ConnectToSource(r.FirstValueOut());
            setRotationFlowIn.MapToControlInput(getMember.assign);
            setRotationFlowOut.MapToControlOutput(getMember.assigned);

            return true;
        }
    }
}