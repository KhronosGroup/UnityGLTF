using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetWorldToLocalMatrixUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.worldToLocalMatrix), new Transform_GetWorldToLocalMatrixUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMemberUnit = unitExporter.unit as GetMember;
            
            var getMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            getMatrix.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
            
            getMatrix.SetupPointerTemplateAndTargetInput(UnitsHelper.IdPointerNodeIndex,
                getMemberUnit.target, "/nodes/{" + UnitsHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);

            var inverse = unitExporter.CreateNode(new Math_InverseNode());
            inverse.ValueIn(Math_InverseNode.IdValueA).ConnectToSource(getMatrix.FirstValueOut());
            
            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(inverse.FirstValueOut());

            SpaceConversionHelpers.AddSpaceConversionNodes(unitExporter, decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation), out var convertedTranslation);
            SpaceConversionHelpers.AddRotationSpaceConversionNodes(unitExporter, decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation), out var convertedRotation);
            
            var compose = unitExporter.CreateNode(new Math_MatComposeNode());
            compose.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(convertedTranslation);
            compose.ValueIn(Math_MatComposeNode.IdInputRotation).ConnectToSource(convertedRotation);
            compose.ValueIn(Math_MatComposeNode.IdInputScale).ConnectToSource(decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale));

            compose.FirstValueOut().MapToPort(getMemberUnit.value);
            
            return true;
            
        }
    }
}