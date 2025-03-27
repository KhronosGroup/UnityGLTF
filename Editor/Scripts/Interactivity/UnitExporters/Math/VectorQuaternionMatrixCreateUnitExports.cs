using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    /// <summary>
    /// Create structs without parameters.
    /// Unlike the .ctor variants with parameters (see GenericMathInvokeUnitExports.cs),
    /// Visual Scripting uses the CreateStruct Unit instead of the InvokeUnit.
    /// </summary>
    public class VectorQuaternionMatrixCreateUnitExports : IUnitExporter
    {
        public Type unitType { get => typeof(CreateStruct); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            CreateStructsUnitExport.RegisterCreateStructConvert(typeof(Vector2), new VectorQuaternionMatrixCreateUnitExports(new Math_Combine2Node()));
            CreateStructsUnitExport.RegisterCreateStructConvert(typeof(Vector3), new VectorQuaternionMatrixCreateUnitExports(new Math_Combine3Node()));
            CreateStructsUnitExport.RegisterCreateStructConvert(typeof(Vector4), new VectorQuaternionMatrixCreateUnitExports(new Math_Combine4Node()));
            CreateStructsUnitExport.RegisterCreateStructConvert(typeof(Quaternion), new VectorQuaternionMatrixCreateUnitExports(new Math_Combine4Node()));
            CreateStructsUnitExport.RegisterCreateStructConvert(typeof(Matrix4x4), new VectorQuaternionMatrixCreateUnitExports(new Math_Combine4x4Node()));
        }

        private GltfInteractivityNodeSchema schema;
        public VectorQuaternionMatrixCreateUnitExports(GltfInteractivityNodeSchema schema)
        {
            this.schema = schema;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as CreateStruct;
            var node = unitExporter.CreateNode(schema);

            foreach (var value in node.ValueSocketConnectionData)
            {
                value.Value.Value = 0f;
                value.Value.Type = GltfTypes.TypeIndexByGltfSignature(GltfTypes.Float);
            }
            
            node.FirstValueOut().MapToPort(unit.valueOutputs[0]);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}