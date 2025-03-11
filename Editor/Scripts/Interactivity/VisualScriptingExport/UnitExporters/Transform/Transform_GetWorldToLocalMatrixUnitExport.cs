using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetWorldToLocalMatrixUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.worldToLocalMatrix), new Transform_GetWorldToLocalMatrixUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMemberUnit = unitExporter.unit as GetMember;
            
            var getMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            getMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            
            getMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                getMemberUnit.target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);

            var inverse = unitExporter.CreateNode(new Math_InverseNode());
            inverse.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(getMatrix.FirstValueOut());

            inverse.FirstValueOut().MapToPort(getMemberUnit.value);
            
            return true;
            
        }
    }
}