using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
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
     
            var getMesh = unitExporter.CreateNode(new Pointer_GetNode());
         
            getMesh.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex, 
                unit.target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/mesh", GltfTypes.Int);
            
            var getMaterial = unitExporter.CreateNode(new Pointer_GetNode());
            unitExporter.MapInputPortToSocketName(Pointer_GetNode.IdValue, getMesh, UnitsHelper.IdPointerMeshIndex, getMaterial);
            
            // TODO: support multiple materials/primitives
            getMaterial.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerMeshIndex,
                "/meshes/{" + UnitsHelper.IdPointerMeshIndex + "}/primitives/0/material", GltfTypes.Int);
            
            unitExporter.MapValueOutportToSocketName(unit.value, Pointer_GetNode.IdValue, getMaterial);
        
            return true;
        }
    }
}