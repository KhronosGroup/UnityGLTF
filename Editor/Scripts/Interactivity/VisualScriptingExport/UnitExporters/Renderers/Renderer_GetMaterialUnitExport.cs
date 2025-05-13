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
         
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getMesh, PointersHelper.IdPointerNodeIndex, 
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/mesh", GltfTypes.Int);
            
            var getMaterial = unitExporter.CreateNode<Pointer_GetNode>();
            getMaterial.ValueIn(PointersHelper.IdPointerMeshIndex).ConnectToSource(getMesh.ValueOut(Pointer_GetNode.IdValue));
            
            // TODO: support multiple materials/primitives
            PointersHelper.SetupPointerTemplateAndTargetInput(getMaterial, PointersHelper.IdPointerMeshIndex,
                "/meshes/{" + PointersHelper.IdPointerMeshIndex + "}/primitives/0/material", GltfTypes.Int);

            getMaterial.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.value);
        
            return true;
        }
    }
}