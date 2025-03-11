using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport
{
    public class Transform_SetScaleUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        private bool worldSpace = false;
        
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.lossyScale), new Transform_SetScaleUnitExport(true));
            SetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localScale), new Transform_SetScaleUnitExport(false));
        }
        
        public Transform_SetScaleUnitExport(bool worldSpace)
        {
            this.worldSpace = worldSpace;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            // TODO: World Space conversion
            
           var unit = unitExporter.unit as Unity.VisualScripting.SetMember;
           
           var setScale = unitExporter.CreateNode(new Pointer_SetNode());
           
           setScale.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
               unit.target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/scale", GltfTypes.Float3);
           
           unitExporter.MapInputPortToSocketName(unit.assign, Pointer_SetNode.IdFlowIn, setScale);
           unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Pointer_SetNode.IdFlowOut, setScale);
           unitExporter.MapInputPortToSocketName(unit.input, Pointer_SetNode.IdValue, setScale);
           return true;
        }
    }
}