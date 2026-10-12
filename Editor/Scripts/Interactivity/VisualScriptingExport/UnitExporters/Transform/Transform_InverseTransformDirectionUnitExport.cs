using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_InverseTransformDirectionUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.InverseTransformDirection), new Transform_InverseTransformDirectionUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var invokeUnit = unitExporter.unit as InvokeMember;
            
            // InverseTransformDirection is not affected by scale or position: rotate the direction by the inverse world rotation
            TransformHelpersVS.GetWorldRotation(unitExporter, invokeUnit.target, out var rotationSocket);
            
            var inverseRotation = unitExporter.CreateNode<Math_QuatConjugateNode>();
            inverseRotation.ValueIn(Math_QuatConjugateNode.IdValueA).ConnectToSource(rotationSocket);
            
            var rotate = unitExporter.CreateNode<Math_Rotate3dNode>();
            rotate.ValueIn(Math_Rotate3dNode.IdInputQuaternion).ConnectToSource(inverseRotation.FirstValueOut());
            
            if (invokeUnit.valueInputs.Count > 2)
            {
                var combine = unitExporter.CreateNode<Math_Combine3Node>();
                combine.ValueIn(Math_Combine3Node.IdValueA).MapToInputPort(invokeUnit.valueInputs["%x"]);
                combine.ValueIn(Math_Combine3Node.IdValueB).MapToInputPort(invokeUnit.valueInputs["%y"]);
                combine.ValueIn(Math_Combine3Node.IdValueC).MapToInputPort(invokeUnit.valueInputs["%z"]);
                rotate.ValueIn(Math_Rotate3dNode.IdInputVector).ConnectToSource(combine.FirstValueOut());
            }
            else
                rotate.ValueIn(Math_Rotate3dNode.IdInputVector).MapToInputPort(invokeUnit.valueInputs["%direction"]);

            rotate.FirstValueOut().MapToPort(invokeUnit.result);
            
            unitExporter.ByPassFlow(invokeUnit.enter, invokeUnit.exit);

            return true;
        }
    }
}
