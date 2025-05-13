using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SetVariableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetVariable); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SetVariableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetVariable;
            
            var node = unitExporter.CreateNode<Variable_SetNode>();
            
            var variableIndex = unitExporter.vsExportContext.AddVariableIfNeeded(unit);

            var variableType = unitExporter.vsExportContext.variables[variableIndex].Type;
            unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Variable_SetNode.IdFlowOut, node);
            
            node.Configuration["variable"].Value = variableIndex;
            
            unitExporter.MapInputPortToSocketName(unit.input, Variable_SetNode.IdInputValue, node);
            unitExporter.MapInputPortToSocketName(unit.assign, Variable_SetNode.IdFlowIn, node);

            node.ValueIn(Variable_SetNode.IdInputValue).SetType(TypeRestriction.LimitToType(variableType));
            bool inputIsLiteral = !unit.input.hasDefaultValue && unit.input.hasValidConnection &&
                                  (unit.input.connections.First().source.unit is Literal 
                                  || unit.input.connections.First().source.unit is Null);
                 
            
            if ( (inputIsLiteral || (unit.input.hasDefaultValue  && !unit.input.hasValidConnection && !unit.input.hasAnyConnection)) && unit.output.hasValidConnection)
            {
                // SetVar has a default value and it's connected to an output,
                // we need to add a GetVar node to ensure the output connected nodes can get the value
                
                var getVarNode = unitExporter.CreateNode<Variable_GetNode>();
                
                getVarNode.Configuration["variable"].Value = variableIndex;
                unitExporter.MapValueOutportToSocketName(unit.output, Variable_GetNode.IdOutputValue, getVarNode);

                getVarNode.OutputValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(variableType);
            }
            else
            if (!unit.input.hasDefaultValue && unit.input.hasValidConnection && unit.output.hasValidConnection)
            {
                unitExporter.ByPassValue(unit.input, unit.output);
            }
            return true;
        }
    }
}
