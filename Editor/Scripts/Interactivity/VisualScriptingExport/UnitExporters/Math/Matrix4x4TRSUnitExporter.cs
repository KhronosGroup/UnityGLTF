using System;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class Matrix4x4TRSUnitExporter : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new Matrix4x4TRSUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as Unity.VisualScripting.InvokeMember;
            var trs = unitExporter.CreateNode<Math_MatComposeNode>();
            trs.ValueIn(Math_MatComposeNode.IdInputTranslation).MapToInputPort(unit.valueInputs["%pos"]);
            trs.ValueIn(Math_MatComposeNode.IdInputRotation).MapToInputPort(unit.valueInputs["%q"]);
            trs.ValueIn(Math_MatComposeNode.IdInputScale).MapToInputPort(unit.valueInputs["%s"]);
           
            trs.ValueOut(Math_MatComposeNode.IdOut).MapToPort(unit.result);
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
    }
}