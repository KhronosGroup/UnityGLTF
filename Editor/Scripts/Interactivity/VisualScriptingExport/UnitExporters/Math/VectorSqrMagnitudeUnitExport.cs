using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class VectorSqrMagnitudeUnitExport : IUnitExporter
    {
        public Type unitType { get; }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector2), nameof(Vector2.sqrMagnitude), new VectorSqrMagnitudeUnitExport());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector3), nameof(Vector3.sqrMagnitude), new VectorSqrMagnitudeUnitExport());
            GetMemberUnitExport.RegisterMemberExporter(typeof(Vector4), nameof(Vector4.sqrMagnitude), new VectorSqrMagnitudeUnitExport());
            
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector2), nameof(Vector2.SqrMagnitude), new VectorSqrMagnitudeUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector3), nameof(Vector3.SqrMagnitude), new VectorSqrMagnitudeUnitExport());
            InvokeUnitExport.RegisterInvokeExporter(typeof(Vector4), nameof(Vector4.SqrMagnitude), new VectorSqrMagnitudeUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {

            var dotNode = unitExporter.CreateNode<Math_DotNode>();
            
            if (unitExporter.unit is GetMember getMember)
            {
                dotNode.FirstValueOut().MapToPort(getMember.value);
                dotNode.ValueIn(Math_DotNode.IdValueA).MapToInputPort(getMember.target);
                dotNode.ValueIn(Math_DotNode.IdValueB).MapToInputPort(getMember.target);
            }
            else
            if (unitExporter.unit is InvokeMember invokeMember)
            {
                if (invokeMember.inputParameters.Count > 0)
                {
                    dotNode.ValueIn(Math_DotNode.IdValueA).MapToInputPort(invokeMember.inputParameters[0]);
                    dotNode.ValueIn(Math_DotNode.IdValueB).MapToInputPort(invokeMember.inputParameters[0]);
                }
                else
                {
                    dotNode.ValueIn(Math_DotNode.IdValueA).MapToInputPort(invokeMember.target);
                    dotNode.ValueIn(Math_DotNode.IdValueB).MapToInputPort(invokeMember.target);
                }
                
                dotNode.FirstValueOut().MapToPort(invokeMember.result);
                
                
                unitExporter.ByPassFlow(invokeMember.enter, invokeMember.exit);
            }

            return true;
        }
    }
}