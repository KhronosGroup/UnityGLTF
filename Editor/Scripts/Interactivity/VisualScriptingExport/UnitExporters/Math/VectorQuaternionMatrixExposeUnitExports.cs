using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class VectorQuaternionMatrixExposeUnitExports : IUnitExporter
    {
        private static readonly string[] VectorMemberIndex = new string[] { "x", "y", "z", "w" };

        private static readonly string[] MatrixMemberIndex = new string[]
        {
            "m00", "m10", "m20", "m30", "m01", "m11", "m21", "m31", "m02", "m12", "m22", "m32", "m03", "m13", "m23",
            "m33"
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
            var matrixMembers = MatrixMemberIndex.Concat(new string[] {nameof(Matrix4x4.lossyScale), nameof(Matrix4x4.rotation)}).ToArray();
            ExposeUnitExport.RegisterExposeConvert(typeof(Matrix4x4), converter, matrixMembers);

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

            bool isMatrix = false;
            if (type == typeof(Vector2))
                schema = new Math_Extract2Node();
            else if (type == typeof(Vector3))
                schema = new Math_Extract3Node();
            else if (type == typeof(Vector4) || type == typeof(Quaternion))
                schema = new Math_Extract4Node();
            else if (type == typeof(Matrix4x4))
            {
                if (exposeUnit != null)
                {
                    var rotation =
                        exposeUnit.members.FirstOrDefault(member => member.Value.name == nameof(Matrix4x4.rotation));
                    var scale = exposeUnit.members.FirstOrDefault(member =>
                        member.Value.name == nameof(Matrix4x4.lossyScale));

                    if (rotation.Key != null || scale.Key != null)
                    {
                        var unit = unitExporter.unit as Expose;
                        var extract = unitExporter.CreateNode(new Math_MatDecomposeNode());
                        extract.ValueIn(Math_MatDecomposeNode.IdInput).MapToInputPort(unit.target);
                        if (rotation.Key != null)
                             extract.ValueOut(Math_MatDecomposeNode.IdOutputRotation).MapToPort(rotation.Key);
                        if (scale.Key != null)
                            extract.ValueOut(Math_MatDecomposeNode.IdOutputScale).MapToPort(scale.Key);
                    }
                }

                isMatrix = true;
                schema = new Math_Extract4x4Node();
            }
            
            if (schema == null)
                return false;
            
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