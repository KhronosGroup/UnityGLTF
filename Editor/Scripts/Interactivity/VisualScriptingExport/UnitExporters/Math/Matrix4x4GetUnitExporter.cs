using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Matrix4x4GetUnitExporter : IUnitExporter
    {
        public Type unitType { get => type; }
        private Type type;
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.GetPosition), new Matrix4x4GetUnitExporter(typeof(InvokeMember)));          
            GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), nameof(Matrix4x4.rotation), new Matrix4x4GetUnitExporter(typeof(GetMember)));           
            GetMemberUnitExport.RegisterMemberExporter(typeof(Matrix4x4), nameof(Matrix4x4.lossyScale), new Matrix4x4GetUnitExporter(typeof(GetMember)));           
        }

        public Matrix4x4GetUnitExporter(Type type)
        {
            this.type = type;
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as MemberUnit;

            var extract = unitExporter.CreateNode<Math_MatDecomposeNode>();
            extract.ValueIn(Math_MatDecomposeNode.IdInput).MapToInputPort(unit.target);

            var resultSocket = unit.valueOutputs[0];
            switch (unit.member.name)
            {
                case nameof(Matrix4x4.GetPosition):
                    extract.ValueOut(Math_MatDecomposeNode.IdOutputTranslation).MapToPort(resultSocket);
                    var invoke = unit as InvokeMember;
                    unitExporter.ByPassFlow(invoke.enter, invoke.exit);
                    break;
                case nameof(Matrix4x4.lossyScale):
                    extract.ValueOut(Math_MatDecomposeNode.IdOutputScale).MapToPort(resultSocket);
                    break;
                case nameof(Matrix4x4.rotation):
                    extract.ValueOut(Math_MatDecomposeNode.IdOutputRotation).MapToPort(resultSocket);
                    break;
            }
            
            return true;
        }
    }
}