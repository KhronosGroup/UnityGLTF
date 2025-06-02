using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GenericMultiplyUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(GenericMultiply); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GenericMultiplyUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GenericMultiply;
            
            var mulNode = unitExporter.CreateNode<Math_MulNode>();
            mulNode.ValueIn("a").MapToInputPort(unit.a);
            mulNode.ValueIn("b").MapToInputPort(unit.b);
            mulNode.FirstValueOut().MapToPort(unit.product);

            // We need to check later - when its available - the input type of the node,
            unitExporter.vsExportContext.OnUnitNodesCreated += nodes =>
            {
                var valueTypeA = unitExporter.vsExportContext.GetValueTypeForInput(mulNode, "a");
                var valueTypeB = unitExporter.vsExportContext.GetValueTypeForInput(mulNode, "b");
                // for Quat * V3 we need to change the Schema to math/rotate3D
                if (valueTypeA == GltfTypes.TypeIndexByGltfSignature("float4")
                    && valueTypeB == GltfTypes.TypeIndexByGltfSignature("float3"))
                {
                    mulNode.SetSchema(new Math_Rotate3dNode(), false);
                    mulNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat4);
                    mulNode.ValueIn("b").SetType(TypeRestriction.LimitToFloat3);
                    mulNode.FirstValueOut().ExpectedType(ExpectedType.Float3);
                }
                else
                // for Matrix4x4 we need to change the Schema to math/matmul
                if (valueTypeB == GltfTypes.TypeIndexByGltfSignature("float4x4"))
                {
                    mulNode.SetSchema(new Math_MatMulNode(), false);
                    mulNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat4x4);
                    mulNode.ValueIn("b").SetType(TypeRestriction.LimitToFloat4x4);
                    mulNode.FirstValueOut().ExpectedType(ExpectedType.Float4x4);
                }
            };
            return true;
        }
    }
}