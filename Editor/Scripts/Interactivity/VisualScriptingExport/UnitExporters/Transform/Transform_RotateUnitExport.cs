using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
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

            var getRotation = unitExporter.CreateNode(new Pointer_GetNode());
            
            PointersHelper.SetupPointerTemplateAndTargetInput(getRotation, PointersHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);

            var setRotation = unitExporter.CreateNode(new Pointer_SetNode());
            
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
                var add = unitExporter.CreateNode(new Math_AddNode());
                
                unitExporter.MapInputPortToSocketName(Pointer_GetNode.IdValue, getRotation,
                    Math_AddNode.IdValueB, add);

                if (unit.valueInputs.Any(vi => vi.type == typeof(Vector3)))
                {
                    // euler is a vector3
                    unitExporter.MapInputPortToSocketName(unit.valueInputs.Skip(1).First(), Math_AddNode.IdValueA,
                        add);
                }
                else
                {
                    // euler is separate floats
                    var combine3 = unitExporter.CreateNode(new Math_Combine3Node());

                    unitExporter.MapInputPortToSocketName(unit.valueInputs[1], "a", combine3);
                    unitExporter.MapInputPortToSocketName(unit.valueInputs[2], "b", combine3);
                    unitExporter.MapInputPortToSocketName(unit.valueInputs[3], "c", combine3);

                    unitExporter.MapInputPortToSocketName("value", combine3, Math_AddNode.IdValueA, add);
  
                }
                unitExporter.MapInputPortToSocketName(Math_AddNode.IdOut, add, Pointer_SetNode.IdValue,
                    setRotation);
            }
            //TODO: translate of non self
            
            unitExporter.MapInputPortToSocketName(unit.enter, Pointer_SetNode.IdFlowIn, setRotation);
            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Pointer_SetNode.IdFlowOut, setRotation);

            PointersHelper.SetupPointerTemplateAndTargetInput(setRotation, PointersHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/rotation", GltfTypes.Float4);


            unitExporter.MapOutFlowConnectionWhenValid(unit.exit, Pointer_SetNode.IdFlowOut, setRotation);
            return true;
        }
    }
}