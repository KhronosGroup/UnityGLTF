using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    [UnitExportPriority(ExportPriority.First)]
    public class Transform_GetChildUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(InvokeMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Transform), nameof(Transform.GetChild)
                , new Transform_GetChildUnitExporter());
            
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;
            
            var getPointer = unitExporter.CreateNode<Pointer_GetNode>();
            getPointer.FirstValueOut().ExpectedType(ExpectedType.Int);
            PointersHelper.SetupPointerTemplateAndTargetInput(getPointer, PointersHelper.IdPointerNodeIndex, "/nodes/{"+PointersHelper.IdPointerNodeIndex+"}/children/{childIndex}", GltfTypes.Int);
            getPointer.ValueIn(PointersHelper.IdPointerNodeIndex).MapToInputPort(unit.target);
            getPointer.ValueIn("childIndex").MapToInputPort(unit.inputParameters[0]);
            getPointer.ValueOut(Pointer_GetNode.IdValue).MapToPort(unit.result);
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }

    }
}