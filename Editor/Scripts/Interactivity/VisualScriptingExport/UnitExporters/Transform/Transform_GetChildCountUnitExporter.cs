using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetChildCountUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.childCount), new Transform_GetChildCountUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;

            var getPointer = unitExporter.CreateNode<Pointer_GetNode>();
            getPointer.FirstValueOut().ExpectedType(ExpectedType.Int);
            PointersHelper.SetupPointerTemplateAndTargetInput(getPointer, PointersHelper.IdPointerNodeIndex, "/nodes/{"+PointersHelper.IdPointerNodeIndex+"}/children.length", GltfTypes.Int);
            getPointer.ValueIn(PointersHelper.IdPointerNodeIndex).MapToInputPort(unit.target);
            getPointer.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.value);
            
            return true;
        }
    }
}