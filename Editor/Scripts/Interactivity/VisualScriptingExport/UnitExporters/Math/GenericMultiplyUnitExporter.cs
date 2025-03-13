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
            
            var mulNode = unitExporter.CreateNode(new Math_MulNode());
            mulNode.ValueIn("a").MapToInputPort(unit.a);
            mulNode.ValueIn("b").MapToInputPort(unit.b);
            mulNode.FirstValueOut().MapToPort(unit.product);

            // We need to check later - when its available - the input type of the node,
            // for Matrix4x4 we need to change the Schema to math/matmul
            unitExporter.exportContext.OnNodesCreated += nodes =>
            {
                var valueType = unitExporter.exportContext.GetValueTypeForInput(mulNode, "a");
                if (valueType == GltfTypes.TypeIndexByGltfSignature("float4x4"))
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