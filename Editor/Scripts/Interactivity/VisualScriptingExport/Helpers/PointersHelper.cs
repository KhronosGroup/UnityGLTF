using Unity.VisualScripting;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class PointersHelperVS : PointersHelper
    {
        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityExportNode node, string pointerId, ValueInput targetInputPort, string pointerTemplate, int valueGltfType)
        {
            PointersHelper.AddPointerConfig(node, pointerTemplate, valueGltfType);
            if (!node.ValueInConnection.ContainsKey(pointerId))
            {
                node.ValueInConnection.Add(pointerId, new GltfInteractivityNode.ValueSocketData());
            }
            
            node.ValueIn(pointerId).MapToInputPort(targetInputPort).SetType(TypeRestriction.LimitToInt);
        }

        public static void SetupPointerTemplateAndTargetInput(GltfInteractivityExportNode node, string pointerId, ValueInput targetInputPort, string pointerTemplate, string gltfType)
        {
            SetupPointerTemplateAndTargetInput(node, pointerId, targetInputPort, pointerTemplate, GltfTypes.TypeIndexByGltfSignature(gltfType));
        }
        
    }
}