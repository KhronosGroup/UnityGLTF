using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    /// <summary>
    /// Exports <see cref="SetVariables"/> as a single variable/set node with multiple variables.
    /// </summary>
    public class SetVariablesUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(SetVariables); }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SetVariablesUnitExport());
        }

        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SetVariables;

            if (unit.kind != VariableKind.Graph && unit.kind != VariableKind.Flow)
            {
                UnitExportLogging.AddErrorLog(unit, "Set Variables only supports Graph and Flow variables.");
                return false;
            }

            var ids = new List<int>();
            for (int i = 0; i < unit.count; i++)
            {
                if (!unitExporter.IsInputLiteralOrDefaultValue(unit.names[i], out var nameValue)
                    || !(nameValue is string variableName) || string.IsNullOrEmpty(variableName))
                {
                    UnitExportLogging.AddErrorLog(unit, $"Variable name {i} must be a literal or default value.");
                    return false;
                }

                var declaration = unit.graph.variables.GetDeclaration(variableName);
                if (declaration == null)
                {
                    UnitExportLogging.AddErrorLog(unit, "Variable not found: " + variableName);
                    return false;
                }

                var id = unitExporter.vsExportContext.AddVariableWithIdIfNeeded(variableName, declaration.value,
                    unit.kind, declaration.typeHandle.Identification);
                if (id == -1 || unitExporter.vsExportContext.variables[id].Type == -1)
                {
                    UnitExportLogging.AddErrorLog(unit, "Type not supported for variable: " + variableName);
                    return false;
                }
                if (ids.Contains(id))
                {
                    UnitExportLogging.AddErrorLog(unit, "Variable is set more than once: " + variableName);
                    return false;
                }
                ids.Add(id);
            }

            var node = unitExporter.CreateNode<Variable_SetNode>();
            node.Configuration[Variable_SetNode.IdConfigVarIndices].Value = ids.ToArray();
            node.FlowIn(Variable_SetNode.IdFlowIn).MapToControlInput(unit.assign);
            unitExporter.MapOutFlowConnectionWhenValid(unit.assigned, Variable_SetNode.IdFlowOut, node);

            for (int i = 0; i < ids.Count; i++)
            {
                var variableType = unitExporter.vsExportContext.variables[ids[i]].Type;
                node.ValueIn(ids[i].ToString())
                    .SetType(TypeRestriction.LimitToType(variableType))
                    .MapToInputPort(unit.values[i]);
            }

            return true;
        }
    }
}
