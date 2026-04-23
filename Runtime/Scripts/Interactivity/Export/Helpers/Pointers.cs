using GLTF.Schema;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class PointersHelper
    {
        
        public static readonly string IdPointerVisibility = $"{IdPointerTemplNodeByIndex}extensions/{KHR_node_visibility_Factory.EXTENSION_NAME}/{nameof(KHR_node_visibility.visible)}"; 
        public static readonly string IdPointerSelectability = $"{IdPointerTemplNodeByIndex}extensions/{KHR_node_selectability_Factory.EXTENSION_NAME}/{nameof(KHR_node_selectability.selectable)}";
        public static readonly string IdPointerVisibilityRef = $"{IdPointerTemplNodeByRef}extensions/{KHR_node_visibility_Factory.EXTENSION_NAME}/{nameof(KHR_node_visibility.visible)}"; 
        public static readonly string IdPointerSelectabilityRef = $"{IdPointerTemplNodeByRef}extensions/{KHR_node_selectability_Factory.EXTENSION_NAME}/{nameof(KHR_node_selectability.selectable)}";

        public static readonly string IdPointerNodeIndex = "nodeIndex";
        public static readonly string IdPointerMeshIndex = "meshIndex";
        public static readonly string IdPointerMaterialIndex = "materialIndex";
        public static readonly string IdPointerAnimationIndex = "animationIndex";
        public static readonly string IdPointerLightIndex = "lightIndex";
        
        public static readonly string IdPointerTemplMaterialByIndex = $"/materials/[{IdPointerMaterialIndex}]/"; 
        public static readonly string IdPointerTemplMeshByIndex = $"/meshes/[{IdPointerMeshIndex}]/"; 
        public static readonly string IdPointerTemplNodeByIndex = $"/nodes/[{IdPointerNodeIndex}]/"; 
        public static readonly string IdPointerTemplLightByIndex = $"/extensions/KHR_lights_punctual/lights/[{IdPointerLightIndex}]/"; 

        
        public static readonly string IdPointerNodeRef = "nodeRef";
        public static readonly string IdPointerMeshRef = "meshRef";
        public static readonly string IdPointerMaterialRef = "materialRef";
        public static readonly string IdPointerAnimationRef = "animationRef";
        public static readonly string IdPointerLightRef = "lightRef";

        public static readonly string IdPointerTemplMaterialByRef = $"/materials/{{{IdPointerMaterialRef}}}/"; 
        public static readonly string IdPointerTemplMeshByRef = $"/meshes/{{{IdPointerMeshRef}}}/"; 
        public static readonly string IdPointerTemplNodeByRef = $"/nodes/{{{IdPointerNodeRef}}}/"; 
        public static readonly string IdPointerTemplLightByRef = $"/extensions/KHR_lights_punctual/lights/{{{IdPointerLightRef}}}/"; 

        
        
        public static void AddPointerTemplateValueInput(GltfInteractivityNode node, string pointerId, int? index = null)
        {
            node.ValueInConnection.Add(pointerId, new GltfInteractivityNode.ValueSocketData()
            {
                Value = index,
                Type = GltfTypes.TypeIndexByGltfSignature("int"),
                typeRestriction = TypeRestriction.LimitToInt,
            });
        }

        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityExportNode node, string pointerId, string pointerTemplate, string valueGltfType)
        {
            SetupPointerTemplateAndTargetInput(node, pointerId, pointerTemplate, GltfTypes.TypeIndexByGltfSignature(valueGltfType));
        }

        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityExportNode node, string pointerId, string pointerTemplate, int valueGltfType)
        {
            AddPointerConfig(node, pointerTemplate, valueGltfType);
            if (!node.ValueInConnection.ContainsKey(pointerId))
                node.ValueInConnection.Add(pointerId, new GltfInteractivityNode.ValueSocketData());
        }
        
        public static void AddPointerConfig(GltfInteractivityNode node, string pointer, string gltfType)
        {
            AddPointerConfig(node, pointer, GltfTypes.TypeIndexByGltfSignature(gltfType));
        }

        public static void AddPointerConfig(GltfInteractivityNode node, string pointer, int gltfType)
        {
            var pointerConfig = node.Configuration[Pointer_SetNode.IdPointer];
            pointerConfig.Value = pointer; 
            var typeConfig = node.Configuration[Pointer_SetNode.IdPointerValueType];
            typeConfig.Value = gltfType; 
        }
    }
}