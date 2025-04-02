using Unity.VisualScripting;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public static class PointersHelper
    {
        public static readonly string IdPointerNodeIndex = "nodeIndex";
        public static readonly string IdPointerMeshIndex = "meshIndex";
        public static readonly string IdPointerMaterialIndex = "materialIndex";
        public static readonly string IdPointerAnimationIndex = "animationIndex";

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
                node.ValueInConnection.Add(pointerId, new GltfInteractivityUnitExporterNode.ValueSocketData());
        }

        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityUnitExporterNode node, string pointerId, ValueInput targetInputPort, string pointerTemplate, int valueGltfType)
        {
            PointersHelper.AddPointerConfig(node, pointerTemplate, valueGltfType);
            if (!node.ValueInConnection.ContainsKey(pointerId))
            {
                node.ValueInConnection.Add(pointerId, new GltfInteractivityNode.ValueSocketData());
            }
            
            node.ValueIn(pointerId).MapToInputPort(targetInputPort).SetType(TypeRestriction.LimitToInt);
        }

        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityUnitExporterNode node, string pointerId, ValueInput targetInputPort, string pointerTemplate, string gltfType)
        {
            SetupPointerTemplateAndTargetInput(node, pointerId, targetInputPort, pointerTemplate, GltfTypes.TypeIndexByGltfSignature(gltfType));
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