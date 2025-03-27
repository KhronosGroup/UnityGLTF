using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VectorQuaternionMatrixSetComponentsUnitExports : IUnitExporter
    {
        public Type unitType { get => typeof(SetMember); }
        
        private static readonly string[] VectorMemberIndex = new string[] { "x", "y", "z", "w" };
        private static readonly string[] InputNames = new string[] {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"};

        
        private static readonly string[] MatrixMemberIndex = new string[]
        {
            "M00", "M10", "M20", "M30", "M01", "M11", "M21", "M31", "M02", "M12", "M22", "M32", "M03", "M13", "M23",
            "M33"
        };
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector2), nameof(Vector2.x), new VectorQuaternionMatrixSetComponentsUnitExports(2, 0) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector2), nameof(Vector2.y), new VectorQuaternionMatrixSetComponentsUnitExports(2, 1) );

            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), nameof(Vector3.x), new VectorQuaternionMatrixSetComponentsUnitExports(3, 0) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), nameof(Vector3.y), new VectorQuaternionMatrixSetComponentsUnitExports(3, 1) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), nameof(Vector3.z), new VectorQuaternionMatrixSetComponentsUnitExports(3, 2) );
            
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector4.x), new VectorQuaternionMatrixSetComponentsUnitExports(4, 0) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector4.y), new VectorQuaternionMatrixSetComponentsUnitExports(4, 1) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector4.z), new VectorQuaternionMatrixSetComponentsUnitExports(4, 2) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector4.w), new VectorQuaternionMatrixSetComponentsUnitExports(4, 3) );
            
            SetMemberUnitExport.RegisterMemberExporter(typeof(Quaternion), nameof(Quaternion.x), new VectorQuaternionMatrixSetComponentsUnitExports(4, 0) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Quaternion), nameof(Quaternion.y), new VectorQuaternionMatrixSetComponentsUnitExports(4, 1) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Quaternion), nameof(Quaternion.z), new VectorQuaternionMatrixSetComponentsUnitExports(4, 2) );
            SetMemberUnitExport.RegisterMemberExporter(typeof(Quaternion), nameof(Quaternion.w), new VectorQuaternionMatrixSetComponentsUnitExports(4, 3) );

            for (int i = 0; i < 16; i++)
                SetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), MatrixMemberIndex[i], new VectorQuaternionMatrixSetComponentsUnitExports(16, i) );
        }

        private int componentCount;
        private int componentIndex;
        
        public VectorQuaternionMatrixSetComponentsUnitExports(int componentCount, int componentIndex)
        {
            this.componentIndex = componentIndex;
            this.componentCount = componentCount;
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var setMember = unitExporter.unit as SetMember;
            
            GltfInteractivityNodeSchema extractSchema = null;
            GltfInteractivityNodeSchema combineSchema = null;
            ExpectedType expectedType = ExpectedType.Float;
            switch (componentCount)
            {
                case 2:
                    extractSchema = new Math_Extract2Node();
                    expectedType = ExpectedType.Float2;
                    combineSchema = new Math_Combine2Node();
                    break;
                case 3:
                    extractSchema = new Math_Extract3Node();
                    expectedType = ExpectedType.Float3;
                    combineSchema = new Math_Combine3Node();
                    break;
                case 4:
                    extractSchema = new Math_Extract4Node();
                    expectedType = ExpectedType.Float4;
                    combineSchema = new Math_Combine4Node();
                    break;
                case 16:
                    extractSchema = new Math_Extract4x4Node();
                    expectedType = ExpectedType.Float4x4;
                    combineSchema = new Math_Combine4x4Node();
                    break;
                default:
                    Debug.LogError("Unsupported component count: " + componentCount);
                    return false;
            }
            
            var extractNode = unitExporter.CreateNode(extractSchema);
            extractNode.ValueIn("a").MapToInputPort(setMember.target);
            
            var combineNode = unitExporter.CreateNode(combineSchema);
            for (int i = 0; i < componentCount; i++)
            {
                if (i == componentIndex)
                    combineNode.ValueIn(InputNames[i]).MapToInputPort(setMember.input);
                else
                    combineNode.ValueIn(InputNames[i]).ConnectToSource(extractNode.ValueOut(i.ToString()));
            }

            combineNode.FirstValueOut().ExpectedType(expectedType);
            
            unitExporter.ByPassValue(setMember.input, setMember.output);
            combineNode.FirstValueOut().MapToPort(setMember.targetOutput);
            unitExporter.ByPassFlow(setMember.assign, setMember.assigned);
            return true;
        }
    }
}