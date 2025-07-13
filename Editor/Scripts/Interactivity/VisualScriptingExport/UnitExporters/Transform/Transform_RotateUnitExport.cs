using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    
    // TODO: actual rotation logic
    
    public class Transform_RotateUnitExport : IUnitExporter
    {
        public Type unitType { get; }


        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.Rotate), new Transform_RotateUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;
            
            bool selfRelative = true;

            if (unit.valueInputs.Contains("%relativeTo"))
            {
                if (unitExporter.IsInputLiteralOrDefaultValue(unit.valueInputs["%relativeTo"], out var relativeToValue))
                {
                    if ((Space)relativeToValue == Space.World)
                        selfRelative = false;
                }
            }
            ValueOutRef objRotationRef;

            void AddRotation(ValueOutRef rotation)
            {
                var mulQuatNode = unitExporter.CreateNode<Math_QuatMulNode>();
                
                if (selfRelative)
                {
                    mulQuatNode.ValueIn(Math_QuatMulNode.IdValueA).ConnectToSource(objRotationRef);
                    mulQuatNode.ValueIn(Math_QuatMulNode.IdValueB).ConnectToSource(rotation);
                    TransformHelpers.SetLocalRotation(unitExporter, out var targetRefSet, out var setRotationRef, out var setRotationFlowIn, out var setRotationFlowOut);
                    targetRefSet.MapToInputPort(unit.target);
                    setRotationRef.ConnectToSource(mulQuatNode.FirstValueOut());
                    setRotationFlowIn.MapToControlInput(unit.enter);
                    setRotationFlowOut.MapToControlOutput(unit.exit);
                }
                else
                {
                    mulQuatNode.ValueIn(Math_QuatMulNode.IdValueA).ConnectToSource(rotation);
                    mulQuatNode.ValueIn(Math_QuatMulNode.IdValueB).ConnectToSource(objRotationRef);
                    TransformHelpers.SetWorldRotation(unitExporter, out var targetRefSet, out var setRotationRef, out var setRotationFlowIn, out var setRotationFlowOut);
                    targetRefSet.MapToInputPort(unit.target);
                    setRotationRef.ConnectToSource(mulQuatNode.FirstValueOut());
                    setRotationFlowIn.MapToControlInput(unit.enter);
                    setRotationFlowOut.MapToControlOutput(unit.exit);
                }
            }
            
            if (selfRelative)
            {
                TransformHelpers.GetLocalRotation(unitExporter, out var targetRef, out objRotationRef);
                targetRef.MapToInputPort(unit.target);
            }
            else
            {
                TransformHelpers.GetWorldRotation(unitExporter, out var targetRef, out objRotationRef);
                targetRef.MapToInputPort(unit.target);
            }
            
            if (unit.valueInputs.Contains("%axis"))
            {
                // Axis / Angle Rotation
                var axis = unit.validInputs.FirstOrDefault(vi => vi.key == "%axis");
                var angle = unit.validInputs.FirstOrDefault(vi => vi.key == "%angle");

                var deg2Rad = unitExporter.CreateNode<Math_RadNode>();
                deg2Rad.ValueIn(Math_RadNode.IdInputA).MapToInputPort(angle);
                
                var axisAngelNode = unitExporter.CreateNode<Math_QuatFromAxisAngleNode>();
                axisAngelNode.ValueIn(Math_QuatFromAxisAngleNode.IdAxis).MapToInputPort(axis);
                axisAngelNode.ValueIn(Math_QuatFromAxisAngleNode.IdAngle).ConnectToSource(deg2Rad.FirstValueOut());

                AddRotation(axisAngelNode.FirstValueOut());
                return true;
            }

            if (unit.valueInputs.Contains("%eulers"))
            {
                QuaternionHelpers.CreateQuaternionFromEuler(unitExporter, out var xyzInputRef, out var euler);
                xyzInputRef.MapToInputPort(unit.valueInputs["%eulers"]);
                AddRotation(euler);
                return true;
            }

            if (unit.valueInputs.Contains("%xAngle"))
            {
                var combine3 = unitExporter.CreateNode<Math_Combine3Node>();
                combine3.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(unit.valueInputs["%xAngle"]);
                combine3.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(unit.valueInputs["%yAngle"]);
                combine3.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(unit.valueInputs["%zAngle"]);

                QuaternionHelpers.CreateQuaternionFromEuler(unitExporter, out var xyzInputRef, out var euler);
                xyzInputRef.ConnectToSource(combine3.FirstValueOut());
                
                AddRotation(euler);
                return true;
            }

            return false;
        }
    }
}