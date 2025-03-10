using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;
using InvokeMember = Unity.VisualScripting.InvokeMember;

namespace UnityGLTF.Interactivity.Export
{
    public class SkinnendMesh_SetWeightsUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.SetBlendShapeWeight), new SkinnendMesh_SetWeightsUnitExport());
        } 
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var setWeight = unitExporter.CreateNode(new Pointer_SetNode());
            
            setWeight.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + GltfInteractivityNodeHelper.IdPointerNodeIndex + "}/weights/{weightIndex}", GltfTypes.Float);

            setWeight.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.enter);
            setWeight.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.exit);
            setWeight.ValueIn("weightIndex").MapToInputPort(unit.valueInputs["%index"]);
            setWeight.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.valueInputs["%value"]);
            return true;
        }
    }
    
    public class SkinnendMesh_GetWeightsUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(SkinnedMeshRenderer), nameof(SkinnedMeshRenderer.GetBlendShapeWeight), new SkinnendMesh_GetWeightsUnitExport());
        } 
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var getWeight = unitExporter.CreateNode(new Pointer_GetNode());
            getWeight.FirstValueOut().ExpectedType(ExpectedType.Float).MapToPort(unit.result);
            
            getWeight.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerNodeIndex,
                unit.target, "/nodes/{" + GltfInteractivityNodeHelper.IdPointerNodeIndex + "}/weights/{weightIndex}", GltfTypes.Float);
            getWeight.ValueIn("weightIndex").MapToInputPort(unit.valueInputs["%index"]);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
    
    public class SkinnendMesh_GetWeightCountUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Mesh), nameof(Mesh.blendShapeCount), new SkinnendMesh_GetWeightCountUnitExporter());
        } 
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
            
            var getWeightCount = unitExporter.CreateNode(new Pointer_GetNode());
            getWeightCount.FirstValueOut().ExpectedType(ExpectedType.Int).MapToPort(unit.value);
            
            getWeightCount.SetupPointerTemplateAndTargetInput(GltfInteractivityNodeHelper.IdPointerMeshIndex,
                unit.target, "/meshes/{" + GltfInteractivityNodeHelper.IdPointerMeshIndex + "}/weights.length", GltfTypes.Int);
            
            return true;
        }
    }
}