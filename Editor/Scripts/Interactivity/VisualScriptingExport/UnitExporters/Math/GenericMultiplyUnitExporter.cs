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
            
            bool IsTypeInput<T>(ValueInput input)
            {
                if (visitedInputs.Contains(input))
                    return false;
                visitedInputs.Add(input);
                
                if (input.type == typeof(T))
                    return true;
                if (input.type == typeof(object))
                    return false;

                if (input.hasDefaultValue)
                {
                    if (input.unit.defaultValues.TryGetValue(input.key, out var defaultValue)
                        && defaultValue is T)
                        return true;
                    return false;
                }

                if (input.hasValidConnection)
                {
                    var source = input.connection.source;
                    if (source.type == typeof(T))
                        return true;
                    
                    foreach (var nextInput in source.unit.valueInputs)
                        if (IsTypeInput<T>(nextInput))
                            return true;
                }

                return false;
            }

            bool IsQuaternionInput(ValueInput input)
            {
                visitedInputs.Clear();
                return IsTypeInput<Quaternion>(input);   
            }

            bool IsMatrix4x4Input(ValueInput input)
            {
                visitedInputs.Clear();
                return IsTypeInput<Matrix4x4>(input);
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
            
            var inputAisMatrix = IsMatrix4x4Input(unit.a);
            var inputBisMatrix = IsMatrix4x4Input(unit.b);
            if (inputAisMatrix && inputBisMatrix)
            {
                var matMulNode = unitExporter.CreateNode<Math_MatMulNode>();
                matMulNode.ValueIn("a").MapToInputPort(unit.a);
                matMulNode.ValueIn("b").MapToInputPort(unit.b);
                matMulNode.FirstValueOut().MapToPort(unit.product);
                return true;
            }
            
            var mulNode = unitExporter.CreateNode<Math_MulNode>();
            mulNode.ValueIn("a").MapToInputPort(unit.a);
            mulNode.ValueIn("b").MapToInputPort(unit.b);
            mulNode.FirstValueOut().MapToPort(unit.product);
            
            // We need to check later - when its available - the input type of the node,
            unitExporter.vsExportContext.OnUnitNodesCreatedStage2 += nodes =>
            {
                var valueTypeA = unitExporter.vsExportContext.GetValueTypeForInput(mulNode, "a");
                var valueTypeB = unitExporter.vsExportContext.GetValueTypeForInput(mulNode, "b");
                // for Quat * V3 we need to change the Schema to math/rotate3D
                if (valueTypeA == GltfTypes.TypeIndexByGltfSignature("float4")
                    && valueTypeB == GltfTypes.TypeIndexByGltfSignature("float3"))
                {
                    mulNode.SetSchema(new Math_Rotate3dNode(), false);
                    var oldSocketA = mulNode.ValueIn("a");
                    var oldSocketB = mulNode.ValueIn("b");
                    mulNode.ValueInConnection.Clear();
                    
                    mulNode.ValueIn(Math_Rotate3dNode.IdInputQuaternion);
                    mulNode.ValueIn(Math_Rotate3dNode.IdInputVector);
                        
                    mulNode.ValueInConnection[Math_Rotate3dNode.IdInputQuaternion] = oldSocketA.socket.Value;
                    mulNode.ValueInConnection[Math_Rotate3dNode.IdInputVector] = oldSocketB.socket.Value;
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