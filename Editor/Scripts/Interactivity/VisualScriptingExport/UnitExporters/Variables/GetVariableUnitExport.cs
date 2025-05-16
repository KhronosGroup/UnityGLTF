using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity.Export;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    // Set priority to first, in case this will create a List/Array, so other nodes can find them
    [UnitExportPriority(ExportPriority.First)]
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
            
            var varValue = unitExporter.vsExportContext.GetVariableValue(unit, out string varName, out string cSharpVarType, false);
            if (varValue != null)
            {
                // Check if the variable is a list/array
                if (varValue.GetType() != typeof(string) && (varValue.GetType().GetInterfaces().Contains(typeof(IEnumerable))))
                {
                    // Check if the list/array is already created
                    var declaration = unitExporter.vsExportContext.GetVariableDeclaration(unit);
                    if (declaration != null)
                    {
                        var existingList = unitExporter.vsExportContext.GetListByCreator(declaration);
                        if (existingList != null)
                        {
                            // List already exist
                            return true;
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
                        var objectList = unitExporter.vsExportContext.CreateNewVariableBasedListFromVariable(declaration, listCapacity, valueTypeIndex);
                        ListHelpersVS.CreateListNodes(unitExporter, objectList);

                        foreach (var v in varValue as IEnumerable)
                        {
                            object value = v;
                            if (v is GameObject go)
                                value = unitExporter.Context.exporter.GetTransformIndex(go.transform);
                            else if (v is Component component)
                                value = unitExporter.Context.exporter.GetTransformIndex(component.transform);
                            else if (v is Material materialValue)
                                value = unitExporter.Context.exporter.GetMaterialIndex(materialValue);

                            objectList.AddItem(value);
                        }
                    }
                    
                    // We cancel here, since we don't want to create a gltf variable for the list
                    return true;
                }
            }
            
            var typeIndex = GltfTypes.TypeIndex(cSharpVarType);
            if (typeIndex == -1)
            {
                UnitExportLogging.AddErrorLog(unit, "Unsupported type");
                return false;
            }
            
            var variableIndex = unitExporter.vsExportContext.AddVariableIfNeeded(unit);
            VariablesHelpers.GetVariable(unitExporter, variableIndex, out var valueSocket);
            valueSocket.MapToPort(unit.value);
            return true;
        }
    }
}