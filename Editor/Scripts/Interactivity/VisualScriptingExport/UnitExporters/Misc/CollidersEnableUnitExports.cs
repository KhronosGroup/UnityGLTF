using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    
    public class CollidersGetEnableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Collider), nameof(Collider.enabled), new CollidersGetEnableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
                
            var selectableNode = unitExporter.CreateNode(new Pointer_GetNode());

            SelectableExtensionHelper.AddExtension(unitExporter, unit, selectableNode);
            
            selectableNode.SetupPointerTemplateAndTargetInput(
                UnitsHelper.IdPointerNodeIndex,
                unit.target, SelectableExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            selectableNode.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.value);
            return true;
        }
    }
    
    public class CollidersSetEnableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Collider), nameof(Collider.enabled), new CollidersSetEnableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetMember;
            var selectableNode = unitExporter.CreateNode(new Pointer_SetNode());
            
            SelectableExtensionHelper.AddExtension(unitExporter, unit, selectableNode);

            selectableNode.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.assign);
            selectableNode.SetupPointerTemplateAndTargetInput(
                UnitsHelper.IdPointerNodeIndex,
                unit.target, SelectableExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            selectableNode.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.input);
            
            selectableNode.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.assigned);
            return true;
        }
    }
}