using System;
using System.Linq;
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
            // TODO: World Space conversion

            var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;

            var getRotation = unitExporter.CreateNode<Pointer_GetNode>();
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            var setRotation = unitExporter.CreateNode<Pointer_SetNode>();
            
            var relative = unit.validInputs.FirstOrDefault(vi => vi.key == "%relativeTo");
            
            if (unit.validInputs.Any(vi => vi.key == "%axis"))
            {
                // Axis / Angle Rotation
                var axis = unit.validInputs.FirstOrDefault(vi => vi.key == "%axis");
                var angle = unit.validInputs.FirstOrDefault(vi => vi.key == "%angle");
                
            }
            else
            {
                // Euler Rotation
                var add = unitExporter.CreateNode<Math_AddNode>();
                
                add.ValueIn(Math_AddNode.IdValueB).ConnectToSource(getRotation.ValueOut(Pointer_GetNode.IdValue));

                if (unit.valueInputs.Any(vi => vi.type == typeof(Vector3)))
                {
                    // euler is a vector3
                    add.ValueIn(Math_AddNode.IdValueA).MapToInputPort(unit.valueInputs.Skip(1).First());
                }
                else
                {
                    // euler is separate floats
                    var combine3 = unitExporter.CreateNode<Math_Combine3Node>();
                    combine3.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(unit.valueInputs[1]);
                    combine3.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(unit.valueInputs[2]);
                    combine3.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(unit.valueInputs[3]);
                    
                    add.ValueIn(Math_AddNode.IdValueA).ConnectToSource(combine3.FirstValueOut());
 
                }
                  //  QuaternionHelpers.CreateQuaternionFromEuler();
                setRotation.ValueIn(Pointer_SetNode.IdValue).ConnectToSource(add.FirstValueOut());
            }
            //TODO: translate of non self
            
            setRotation.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.enter);
            setRotation.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.exit);
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            return true;
        }
    }
}