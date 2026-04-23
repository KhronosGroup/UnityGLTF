using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Renderer_GetMaterialUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Renderer), nameof(Renderer.material), new Renderer_GetMaterialUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
     
            var getMesh = unitExporter.CreateNode<Pointer_GetNode>();
         
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getMesh, PointersHelper.IdPointerNodeRef, 
                unit.target, PointersHelper.IdPointerTemplNodeByRef + "mesh", GltfTypes.Ref);
            
            var getMaterial = unitExporter.CreateNode<Pointer_GetNode>();
            getMaterial.ValueIn(PointersHelper.IdPointerMeshRef).ConnectToSource(getMesh.ValueOut(Pointer_GetNode.IdValue));
            
            // TODO: support multiple materials/primitives
            PointersHelper.SetupPointerTemplateAndTargetInput(getMaterial, PointersHelper.IdPointerMeshRef,
                PointersHelper.IdPointerTemplMeshByRef + "primitives/0/material", GltfTypes.Ref);

            getMaterial.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.value);
        
            return true;
        }
    }
}