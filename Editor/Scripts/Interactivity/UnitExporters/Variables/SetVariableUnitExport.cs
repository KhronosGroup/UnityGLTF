using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
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
            
            var node = unitExporter.CreateNode(new Variable_SetNode());
            
            var variableIndex = unitExporter.exportContext.AddVariableIfNeeded(unit);

            var variableType = unitExporter.exportContext.variables[variableIndex].Type;
            unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Variable_SetNode.IdFlowOut, node);
            
            node.ConfigurationData["variable"].Value = variableIndex;
            
            unitExporter.MapInputPortToSocketName(unit.input, Variable_SetNode.IdInputValue, node);
            unitExporter.MapInputPortToSocketName(unit.assign, Variable_SetNode.IdFlowIn, node);

            bool inputIsLiteral = !unit.input.hasDefaultValue && unit.input.hasValidConnection &&
                                  (unit.input.connections.First().source.unit is Literal 
                                  || unit.input.connections.First().source.unit is Null);
                 
            
            if ( (inputIsLiteral || (unit.input.hasDefaultValue  && !unit.input.hasValidConnection && !unit.input.hasAnyConnection)) && unit.output.hasValidConnection)
            {
                // SetVar has a default value and it's connected to an output,
                // we need to add a GetVar node to ensure the output connected nodes can get the value
                
                var getVarNode = unitExporter.CreateNode(new Variable_GetNode());
                
                getVarNode.ConfigurationData["variable"].Value = variableIndex;
                unitExporter.MapValueOutportToSocketName(unit.output, Variable_GetNode.IdOutputValue, getVarNode);

                getVarNode.OutValueSocket[Variable_GetNode.IdOutputValue].expectedType = ExpectedType.GtlfType(variableType);
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
