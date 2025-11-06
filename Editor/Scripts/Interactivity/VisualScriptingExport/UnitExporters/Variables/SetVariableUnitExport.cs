using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
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
            
            
            var variableIndex = unitExporter.vsExportContext.AddVariableIfNeeded(unit);
            var node = VariablesHelpers.SetVariable(unitExporter, variableIndex, out var valueSocket, out var flowIn, out var flowOut);

            var variableType = unitExporter.vsExportContext.variables[variableIndex].Type;
            unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Variable_SetNode.IdFlowOut, node);
            
            node.Configuration["variable"].Value = variableIndex;

            valueSocket.MapToInputPort(unit.input);
            flowIn.MapToControlInput(unit.assign);

            bool inputIsLiteral = !unit.input.hasDefaultValue && unit.input.hasValidConnection &&
                                  (unit.input.connections.First().source.unit is Literal 
                                  || unit.input.connections.First().source.unit is Null);
                 
            
            if ( (inputIsLiteral || (unit.input.hasDefaultValue  && !unit.input.hasValidConnection && !unit.input.hasAnyConnection)) && unit.output.hasValidConnection)
            {
                // SetVar has a default value and it's connected to an output,
                // we need to add a GetVar node to ensure the output connected nodes can get the value
                
                var getVarNode = unitExporter.CreateNode<Variable_GetNode>();
                
                getVarNode.Configuration[Variable_GetNode.IdConfigVarIndex].Value = variableIndex;
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
