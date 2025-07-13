using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
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
            HashSet<ValueInput> visitedInputs = new HashSet<ValueInput>();
            
            bool IsQuaternionInput(ValueInput input)
            {
                if (visitedInputs.Contains(input))
                    return false;
                visitedInputs.Add(input);
                
                if (input.type == typeof(Quaternion))
                    return true;

                if (input.hasDefaultValue)
                {
                    if (input.unit.defaultValues.TryGetValue(input.key, out var defaultValue)
                        && defaultValue is Quaternion)
                        return true;
                    return false;
                }

                if (input.hasValidConnection)
                {
                    var source = input.connection.source;
                    if (source.type == typeof(Quaternion))
                        return true;
                    
                    foreach (var nextInput in source.unit.valueInputs)
                        if (IsQuaternionInput(nextInput))
                            return true;
                }

                return false;
            }
            
            var unit = unitExporter.unit as GenericMultiply;

            var inputAIsQuat = IsQuaternionInput(unit.a);
            var inputBIsQuat = IsQuaternionInput(unit.b);
            
            if (inputAIsQuat && inputBIsQuat)
            {
                    var quatMulNode = unitExporter.CreateNode<Math_QuatMulNode>();
                    quatMulNode.ValueIn("a").MapToInputPort(unit.a);
                    quatMulNode.ValueIn("b").MapToInputPort(unit.b);
                    quatMulNode.FirstValueOut().MapToPort(unit.product);
                return true;
            }
            
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
                    mulNode.ValueIn("b").SetType(TypeRestriction.LimitToFloat4);
                    mulNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat3);
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
                else
                if (valueTypeA == GltfTypes.TypeIndexByGltfSignature("float4")
                    && valueTypeB == GltfTypes.TypeIndexByGltfSignature("float4")
                    && (inputAIsQuat || inputBIsQuat))
                {
                    mulNode.SetSchema(new Math_QuatMulNode(), false);
                    mulNode.ValueIn("a").SetType(TypeRestriction.LimitToFloat4);
                    mulNode.ValueIn("b").SetType(TypeRestriction.LimitToFloat4);
                    mulNode.FirstValueOut().ExpectedType(ExpectedType.Float4);
                }
            };
            return true;
        }
    }
}