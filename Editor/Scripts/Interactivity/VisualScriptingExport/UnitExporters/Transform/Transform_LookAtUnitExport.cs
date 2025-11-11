using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_LookAtUnitExport : IUnitExporter
    {
        public Type unitType { get; }


        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.LookAt), new Transform_LookAtUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;
            
            /*
             * LookAt Overloads:
             *  (Transform target)
             *  (Transform target, Vector3 worldUp)
             *  (Vector3 worldPosition)
             *  (Vector3 worldPosition, Vector3 worldUp)
             */

            ValueInRef worldUpRef = null;
            
            TransformHelpers.GetWorldPosition(unitExporter, out var worldPosSelfTargetRef, out var worldPosSelfRef);
            worldPosSelfTargetRef.MapToInputPort(unit.target);
            
            var subNode = unitExporter.CreateNode<Math_SubNode>();
            var worldPosInput= subNode.ValueIn("a");
            subNode.ValueIn("a").ConnectToSource(worldPosSelfRef);
            

            if (unit.valueInputs.Contains("%target"))
            {
                // Using target Transform variant
                TransformHelpers.GetWorldPosition(unitExporter, out var worldPosTargetRef, out var worldPosRef);
                worldPosTargetRef.MapToInputPort(unit.valueInputs["%target"]);
                worldPosInput.ConnectToSource(worldPosRef);
            }
            else
            {
                // Using world position variant
                worldPosInput.MapToInputPort(unit.valueInputs["%worldPosition"]);
            }
            
            var normalizeNode = unitExporter.CreateNode<Math_NormalizeNode>();
            normalizeNode.FirstValueIn().ConnectToSource(subNode.FirstValueOut());
            
            var lookRotNode = unitExporter.CreateNode<Math_QuatFromUpForwardNode>();
            worldUpRef = lookRotNode.ValueIn(Math_QuatFromUpForwardNode.IdUp);
            
            if (unit.valueInputs.Contains("%worldUp"))
                worldUpRef.MapToInputPort(unit.valueInputs["%worldUp"]);
            else
                worldUpRef.SetValue(Vector3.up);
            
            TransformHelpers.SetWorldRotation(unitExporter, out var targetRef, out var setRotationRef, out var setRotationFlowIn, out var setRotationFlowOut);
            targetRef.MapToInputPort(unit.target);
            setRotationRef.ConnectToSource(lookRotNode.FirstValueOut());
            setRotationFlowIn.MapToControlInput(unit.enter);
            setRotationFlowOut.MapToControlOutput(unit.exit);
            
            return true;
        }
    }
}