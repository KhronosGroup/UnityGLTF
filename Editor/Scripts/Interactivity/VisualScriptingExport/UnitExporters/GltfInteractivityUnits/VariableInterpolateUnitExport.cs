using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting;
using UnityGLTF.Interactivity.VisualScripting.Export;
using UnityGLTF.Interactivity.Schema;

namespace Editor.UnitExporters.GltfInteractivityUnits
{
    public class VariableInterpolateUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(VariableInterpolate); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new VariableInterpolateUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as VariableInterpolate;
            
            var node = unitExporter.CreateNode(new Variable_InterpolateNode());

            var varDeclaration = unitExporter.exportContext.GetVariableDeclaration(unit);
            if (varDeclaration == null)
            {
                UnitExportLogging.AddErrorLog(unit, "Could not find variable declaration");
                return false;
            }
            
            var useSlerp = false;
            var varId = unitExporter.exportContext.AddVariableIfNeeded(unit);
            node.Configuration[Variable_InterpolateNode.IdConfigVariable].Value = varId;
            node.Configuration[Variable_InterpolateNode.IdUseSlerp].Value = useSlerp;
            node.FlowIn(Variable_InterpolateNode.IdFlowIn).MapToControlInput(unit.assign);
            node.FlowOut(Variable_InterpolateNode.IdFlowOut).MapToControlOutput(unit.assigned);
            
            node.ValueIn(Variable_InterpolateNode.IdValue).MapToInputPort(unit.targetValue);
            node.ValueIn(Variable_InterpolateNode.IdDuration).MapToInputPort(unit.duration);
            node.ValueIn(Variable_InterpolateNode.IdPoint1).MapToInputPort(unit.pointA);
            node.ValueIn(Variable_InterpolateNode.IdPoint2).MapToInputPort(unit.pointB);
            node.FlowOut(Variable_InterpolateNode.IdFlowOutDone).MapToControlOutput(unit.done);
            
            return true;
        }
    }
}