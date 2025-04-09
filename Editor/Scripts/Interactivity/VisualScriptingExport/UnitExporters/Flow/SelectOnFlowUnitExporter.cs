using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class SelectOnFlowUnitExporter : IUnitExporter
    {
        public Type unitType { get => typeof(SelectOnFlow); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new SelectOnFlowUnitExporter());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as SelectOnFlow;

            if (unit.branchCount == 0)
            {
                UnitExportLogging.AddWarningLog(unit, "No branches. Will be skipped.");
                return false;
            }
            
            var setVarNodes = new List<GltfInteractivityExportNode>();
            
            // Temporary set a Type Index, will be updated later in OnNodesCreated callback,
            // when it's possible to identify the type from then connected inputs
            int typeIndex = 0;
            
            // using VariableKind.Scene, because we already generated a unique name for the variable
            var varIndex = unitExporter.vsExportContext.AddVariableWithIdIfNeeded($"SelectOnFlowValue_{GUID.Generate().ToString()}", null, VariableKind.Scene, typeIndex);

            var getVar = VariablesHelpers.GetVariable(unitExporter, varIndex, out var getVarValue);
            getVarValue.MapToPort(unit.selection);
            
            for (int i = 0; i < unit.branchCount; i++)
            {
                var setVar = VariablesHelpersVS.SetVariable(unitExporter, varIndex, unit.valueInputs[i], unit.controlInputs[i], unit.exit);
                setVarNodes.Add(setVar);
                setVar.ValueIn(Variable_SetNode.IdInputValue).socket.Value.Type = -1;
            }

            void PostTypeResolving(bool lastTry = false)
            {
                int typeIndex = -1;
                foreach (var setVarNode in setVarNodes)
                {
                    typeIndex = unitExporter.vsExportContext.GetValueTypeForInput(setVarNode, Variable_SetNode.IdInputValue);
                    if (typeIndex != -1)
                        break;
                }

                if (typeIndex == -1)
                {
                    if (lastTry)
                        UnitExportLogging.AddErrorLog(unit, "Can't resolve input type.");
                    // We don't cancel here, to trigger later the validation process for invalid type index
                }
                unitExporter.vsExportContext.variables[varIndex].Type = typeIndex;
                if (typeIndex == -1)
                    return;
                
                getVar.ValueOut(Variable_GetNode.IdOutputValue).ExpectedType(ExpectedType.GtlfType(typeIndex));
                foreach (var n in setVarNodes)
                {
                    n.ValueIn(Variable_SetNode.IdInputValue).SetType(TypeRestriction.LimitToType(typeIndex));
                }
            }
            
            // Update Type Index
            unitExporter.vsExportContext.OnUnitNodesCreated += (nodes => { PostTypeResolving(); });
            unitExporter.vsExportContext.OnBeforeSerialization += (nodes) => { PostTypeResolving(true); };
            return true;
        }
    }
}