using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GetMeshUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(MeshFilter), nameof(MeshFilter.mesh), new GetMeshUnitExport());
            GetMemberUnitExport.RegisterMemberExporter(typeof(MeshFilter), nameof(MeshFilter.sharedMesh), new GetMeshUnitExport());
            GetMemberUnitExport.RegisterMemberExporter(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.sharedMesh), new GetMeshUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
     
            var getMesh = unitExporter.CreateNode<Pointer_GetNode>();
         
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getMesh, PointersHelper.IdPointerNodeIndex, 
                unit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/mesh", GltfTypes.Int);

            getMesh.FirstValueOut().MapToPort(unit.value);
            
            return true;
        }
    }
}