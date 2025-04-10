using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Transform_GetLocalToWorldMatrixUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetMember); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Transform), nameof(Transform.localToWorldMatrix), new Transform_GetLocalToWorldMatrixUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var getMemberUnit = unitExporter.unit as GetMember;
            
            var getMatrix = unitExporter.CreateNode(new Pointer_GetNode());
            
            PointersHelperVS.SetupPointerTemplateAndTargetInput(getMatrix, PointersHelper.IdPointerNodeIndex,
                getMemberUnit.target, "/nodes/{" + PointersHelper.IdPointerNodeIndex + "}/globalMatrix", GltfTypes.Float4x4);

            var decompose = unitExporter.CreateNode(new Math_MatDecomposeNode());
            decompose.ValueIn(Math_MatDecomposeNode.IdInput).ConnectToSource(getMatrix.FirstValueOut());

            var compose = unitExporter.CreateNode(new Math_MatComposeNode());
            if (unitExporter.Context.addUnityGltfSpaceConversion)
            {
                SpaceConversionHelpers.AddSpaceConversion(unitExporter, decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation), out var convertedTranslation);
                SpaceConversionHelpers.AddRotationSpaceConversion(unitExporter, decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation), out var convertedRotation);
                
                compose.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(convertedTranslation);
                compose.ValueIn(Math_MatComposeNode.IdInputRotation).ConnectToSource(convertedRotation);
            }
            else
            {
                compose.ValueIn(Math_MatComposeNode.IdInputTranslation).ConnectToSource(decompose.ValueOut(Math_MatDecomposeNode.IdOutputTranslation));
                compose.ValueIn(Math_MatComposeNode.IdInputRotation).ConnectToSource(decompose.ValueOut(Math_MatDecomposeNode.IdOutputRotation));
            }
            
            compose.ValueIn(Math_MatComposeNode.IdInputScale).ConnectToSource(decompose.ValueOut(Math_MatDecomposeNode.IdOutputScale));

            compose.FirstValueOut().MapToPort(getMemberUnit.value);
            return true;
        }
    }
}