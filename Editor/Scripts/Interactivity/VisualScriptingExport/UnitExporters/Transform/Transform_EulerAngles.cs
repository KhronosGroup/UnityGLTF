using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_EulerAngles : IUnitExporter
    {
        public Type unitType { get; }

        private bool worldSpace = false;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.eulerAngles), new Transform_EulerAngles(true));
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localEulerAngles), new Transform_EulerAngles(false));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.eulerAngles), new Transform_EulerAngles(true));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localEulerAngles), new Transform_EulerAngles(false));
        }

        public Transform_EulerAngles(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            if (unitExporter.unit is Unity.VisualScripting.GetMember getMember)
            {
                ValueOutRef rotation;
                if (worldSpace)
                    TransformHelpersVS.GetWorldRotation(unitExporter, getMember.target, out rotation);
                else
                    TransformHelpersVS.GetLocalRotation(unitExporter, getMember.target, out rotation);

                QuaternionHelpers.QuaternionToEuler(unitExporter, rotation, out var euler);
                euler.MapToPort(getMember.value);
                return true;
            }

            var unit = unitExporter.unit as Unity.VisualScripting.SetMember;
            QuaternionHelpersVS.CreateQuaternionFromEuler(unitExporter, unit.input, out var quaternion);

            if (worldSpace)
            {
                TransformHelpers.SetWorldRotation(unitExporter, out var targetRef, out var rotationRef, out var flowIn, out var flowOut);
                targetRef.MapToInputPort(unit.target);
                rotationRef.ConnectToSource(quaternion);
                flowIn.MapToControlInput(unit.assign);
                flowOut.MapToControlOutput(unit.assigned);
            }
            else
                TransformHelpersVS.SetLocalRotation(unitExporter, unit.target, quaternion, unit.assign, unit.assigned);
            return true;
        }
    }
}
