using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class ColorUnitExport : IUnitExporter
    {
        public Type unitType { get; }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(Color), ".ctor", new ColorUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;


            var colorNode = unitExporter.CreateNode<Math_Combine4Node>();
            colorNode.ValueIn(Math_Combine4Node.IdValueA).MapToInputPort(unit.valueInputs["%r"]);
            colorNode.ValueIn(Math_Combine4Node.IdValueB).MapToInputPort(unit.valueInputs["%g"]);
            colorNode.ValueIn(Math_Combine4Node.IdValueC).MapToInputPort(unit.valueInputs["%b"]);
            if (unit.valueInputs.Contains("%a"))
                colorNode.ValueIn(Math_Combine4Node.IdValueA).MapToInputPort(unit.valueInputs["%a"]);
            else
                colorNode.ValueIn(Math_Combine4Node.IdValueD).SetValue(1f);

            colorNode.FirstValueOut().MapToPort(unit.result);

            return true;
        }
    }
}