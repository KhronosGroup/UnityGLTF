using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class GameObject_SetActiveUnitExport : IUnitExporter
    {
        public System.Type unitType { get => typeof(InvokeMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(GameObject), nameof(GameObject.SetActive),
                new GameObject_SetActiveUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var visibleNode = unitExporter.CreateNode(new Pointer_SetNode());
            var selectableNode = unitExporter.CreateNode(new Pointer_SetNode());

            VisibleExtensionHelper.AddExtension(unitExporter, unit, visibleNode);
            SelectableExtensionHelper.AddExtension(unitExporter, unit, selectableNode);
            
            visibleNode.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.enter);
            
            visibleNode.SetupPointerTemplateAndTargetInput(
                GltfInteractivityNodeHelper.IdPointerNodeIndex,
                unit.target, VisibleExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            
            visibleNode.FlowOut(Pointer_SetNode.IdFlowOut)
                .ConnectToFlowDestination(selectableNode.FlowIn(Pointer_SetNode.IdFlowIn));
            selectableNode.SetupPointerTemplateAndTargetInput(
                GltfInteractivityNodeHelper.IdPointerNodeIndex,
                unit.target, SelectableExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            
            selectableNode.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.exit);
            return true;
        }
    }
}