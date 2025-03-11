using System;
using System.Collections;
using System.Linq;
using Editor.UnitExporters.Lists;
using Unity.VisualScripting;
using UnityEditor;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public class GetVariableUnitExport : IUnitExporter
    {
        public Type unitType { get => typeof(GetVariable); }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UnitExporterRegistry.RegisterExporter(new GetVariableUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as GetVariable;
            
            var varValue = unitExporter.exportContext.GetVariableValue(unit, out string varName, out string varType, false);
            if (varValue != null)
            {
                // Check if the variable is a list/array
                if (varValue.GetType() != typeof(string) && (varValue.GetType().GetInterfaces().Contains(typeof(IEnumerable))))
                {
                    // Check if the list/array is already created
                    var declaration = unitExporter.exportContext.GetVariableDeclaration(unit);
                    if (declaration != null)
                    {
                        var existingList = unitExporter.exportContext.GetListByCreator(declaration);
                        if (existingList != null)
                        {
                            // List already exist
                            
                            // TODO: because we don't create any node here,
                            // we would get a false-error message that this Unit could not be exported.
                            // We would need to add a Return Bool for error handling in UnitExporter in the future 
                            return false;
                        }
                        
                        // Create list
                        var listCapacity = 0;
                        var valueTypeIndex = -1;
                        if (varValue.GetType().IsArray)
                        {
                            listCapacity = (varValue as Array).Length;
                            valueTypeIndex = GltfTypes.TypeIndex(varValue.GetType().GetElementType());
                        }
                        else
                        {
                            var varValueType = varValue.GetType();
                            var elementType = varValueType.GenericTypeArguments[0];
                            valueTypeIndex = GltfTypes.TypeIndex(elementType);
                            var countProperty = varValueType.GetProperty("Count");
                            if (countProperty != null)
                                listCapacity = (int)countProperty.GetValue(varValue);
                            else
                            {
                                var lengthProperty = varValueType.GetProperty("Length");
                                if (lengthProperty != null)
                                    listCapacity = (int)lengthProperty.GetValue(varValue);
                            }
                        }
                        
                        if (listCapacity == 0)
                        {
                            UnitExportLogging.AddErrorLog(unit, "List/Array requires at least 1 element. The exported list capacity will be limited to the existing length.");
                            return false;
                        }
                        
                        if (valueTypeIndex == -1)
                        {
                            UnitExportLogging.AddErrorLog(unit, "Unsupported list type");
                            return false;
                        }
                        var objectList = unitExporter.exportContext.CreateNewVariableBasedListFromVariable(declaration, listCapacity, valueTypeIndex);
                        ListHelpers.CreateListNodes(unitExporter, objectList);

                        foreach (var v in varValue as IEnumerable)
                            objectList.AddItem(v);
                    }
                    
                    // We cancel here, since we don't want to create a gltf variable for the list
                    return false;
                }
            }
            
            var typeIndex = GltfTypes.TypeIndex(varType);
            if (typeIndex == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Unsupported type");
                return false;
            }
            
            var variableIndex = unitExporter.exportContext.AddVariableIfNeeded(unit);
            VariablesHelpers.GetVariable(unitExporter, variableIndex, out var valueSocket);
            valueSocket.MapToPort(unit.value);
            return true;
        }
    }
}