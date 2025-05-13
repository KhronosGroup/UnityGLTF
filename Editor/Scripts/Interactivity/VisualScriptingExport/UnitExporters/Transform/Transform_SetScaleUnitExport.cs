using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_SetScaleUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localScale), new Transform_SetScaleUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
           var unit = unitExporter.unit as Unity.VisualScripting.SetMember;
           
           var setScale = unitExporter.CreateNode<Pointer_SetNode>();
           
           PointersHelperVS.SetupPointerTemplateAndTargetInput(setScale, PointersHelper.IdPointerNodeIndex,
               unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/scale", GltfTypes.Float3);
           
           unitExporter.MapInputPortToSocketName(unit.assign, Pointer_SetNode.IdFlowIn, setScale);
           unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Pointer_SetNode.IdFlowOut, setScale);
           unitExporter.MapInputPortToSocketName(unit.input, Pointer_SetNode.IdValue, setScale);
           return true;
        }
    }
}