using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    
    public class RendererGetEnableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Renderer), nameof(Renderer.enabled), new RendererGetEnableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetMember;
                
            var visibleNode = unitExporter.CreateNode<Pointer_GetNode>();

            VisibleExtensionHelper.AddExtension(unitExporter, unit, visibleNode);
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(visibleNode,
                PointersHelper.IdPointerNodeIndex,
                unit.target, VisibleExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            visibleNode.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.value);
            return true;
        }
    }
    
    public class RendererSetEnableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Renderer), nameof(Renderer.enabled), new RendererSetEnableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetMember;
            var selectableNode = unitExporter.CreateNode<Pointer_SetNode>();
            
            VisibleExtensionHelper.AddExtension(unitExporter, unit, selectableNode);

            selectableNode.FlowIn(Pointer_SetNode.IdFlowIn).MapToControlInput(unit.assign);
            PointersHelperVS.SetupPointerTemplateAndTargetInput(selectableNode,
                PointersHelper.IdPointerNodeIndex,
                unit.target, VisibleExtensionHelper.PointerTemplate,
                GltfTypes.Bool);
            selectableNode.ValueIn(Pointer_SetNode.IdValue).MapToInputPort(unit.input);
            
            selectableNode.FlowOut(Pointer_SetNode.IdFlowOut).MapToControlOutput(unit.assigned);
            return true;
        }
    }
}