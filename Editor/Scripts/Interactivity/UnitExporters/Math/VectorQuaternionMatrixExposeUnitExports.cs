using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public class VectorQuaternionMatrixExposeUnitExports : IUnitExporter
    {
        private static readonly string[] VectorMemberIndex = new string[] { "x", "y", "z", "w" };

        private static readonly string[] MatrixMemberIndex = new string[]
        {
            "M00", "M10", "M20", "M30", "M01", "M11", "M21", "M31", "M02", "M12", "M22", "M32", "M03", "M13", "M23",
            "M33"
        };


        public Type unitType
        {
            get => typeof(Expose);
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            var converter = new VectorQuaternionMatrixExposeUnitExports();
            ExposeUnitExport.RegisterExposeConvert(typeof(Vector2), converter, "x", "y");
            ExposeUnitExport.RegisterExposeConvert(typeof(Vector3), converter, "x", "y", "z");
            ExposeUnitExport.RegisterExposeConvert(typeof(Vector4), converter, "x", "y", "z", "w");
            ExposeUnitExport.RegisterExposeConvert(typeof(Matrix4x4), converter, MatrixMemberIndex);

            ExposeUnitExport.RegisterExposeConvert(typeof(Quaternion), converter, "x", "y", "z", "w");

            for (int i = 0; i < 2; i++)
                GetMemberUnitExport.RegisterMemberExporter(typeof(Vector2), VectorMemberIndex[i], converter);

            for (int i = 0; i < 3; i++)
                GetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), VectorMemberIndex[i], converter);

            for (int i = 0; i < 4; i++)
            {
                GetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), VectorMemberIndex[i], converter);
                GetMemberUnitExport.RegisterMemberExporter(typeof(Quaternion), VectorMemberIndex[i], converter);
                
            }

            for (int i = 0; i < MatrixMemberIndex.Length; i++)
                GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), MatrixMemberIndex[i], converter);
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var exposeUnit = unitExporter.unit as Expose;
            var getMemberNode = unitExporter.unit as GetMember;
            
            GltfInteractivityNodeSchema schema = null;
            Type type = null;
            ValueInput target = null;
            
            if (exposeUnit != null)
            {
                type = exposeUnit.type;
                target = exposeUnit.target;
            }
            else if (getMemberNode != null)
            {
                type = getMemberNode.target.type;
                target = getMemberNode.target;
            }
            
            if (type == typeof(Vector2))
                schema = new Math_Extract2Node();
            else if (type == typeof(Vector3))
                schema = new Math_Extract3Node();
            else if (type == typeof(Vector4) || type == typeof(Quaternion))
                schema = new Math_Extract4Node();
            else if (type == typeof(Matrix4x4))
                schema = new Math_Extract4x4Node();
            
            if (schema == null)
                return false;

            bool isMatrix = schema.OutputValueSockets.Count == 16;
            
            var node = unitExporter.CreateNode(schema);
            
            unitExporter.MapInputPortToSocketName(target, Math_Extract2Node.IdValueIn, node);

            void AddMember(IUnitOutputPort port, Member member)
            {
                if (isMatrix)
                {
                    for (int i = 0; i < MatrixMemberIndex.Length; i++)
                    {
                        if (member.name == MatrixMemberIndex[i])
                        {
                            unitExporter.MapValueOutportToSocketName(port, i.ToString(), node);
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < VectorMemberIndex.Length; i++)
                    {
                        if (member.name == VectorMemberIndex[i])
                        {
                            unitExporter.MapValueOutportToSocketName(port, i.ToString(), node);
                            break;
                        }
                    }
                } 
            }
            
                
            if (getMemberNode != null)
                AddMember(getMemberNode.value, getMemberNode.member);

            if (exposeUnit != null)
            {
                foreach (var outport in exposeUnit.members)
                    AddMember(outport.Key, outport.Value);
            }
            return true;
        }
    }
}