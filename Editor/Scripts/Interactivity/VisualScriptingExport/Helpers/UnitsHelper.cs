using UnityGLTF.Interactivity.VisualScripting.Schema;
using UnityGLTF.Interactivity.VisualScripting.VisualScriptingExport;

namespace UnityGLTF.Interactivity.VisualScripting
{
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using UnityEngine;
    using Unity.VisualScripting;

    public static class UnitsHelper
    {
        
        public static Dictionary<IUnit, UnitExporter> GetTranslatableUnits(
            IEnumerable<IUnit> sortedNodes, VisualScriptingExportContext exportContext)
        {
            Dictionary<IUnit, UnitExporter> validNodes =
                new Dictionary<IUnit, UnitExporter>();

            foreach (IUnit unit in sortedNodes)
            {
                if (unit is Literal || unit is This || unit is Null)
                    continue;
                
                UnitExporter unitExporter = UnitExporterRegistry.CreateUnitExporter(exportContext, unit);
                if (unitExporter != null)
                {
                    if (unitExporter.IsTranslatable && unitExporter.Nodes.Length > 0)
                        validNodes.Add(unit, unitExporter);
                }
                else
                     Debug.LogWarning("ExportNode is null for unit: " + Log(unit)+ " of type: " + unit.GetType());
            }

            return validNodes;
        }

        static string Log(IUnit unit)
        {
            if (unit is InvokeMember invokeMember)
                return unit.ToString() + " Invoke: " + invokeMember.member.declaringType + "." + invokeMember.member.name;
            if (unit is SetMember setMember)
                return unit.ToString() + " Set: " + setMember.target + "." + setMember.member.name;
            if (unit is GetMember getMember)
                return unit.ToString() + " Get: " + getMember.target + "." + getMember.member.name;
            if (unit is Expose expose)
                return unit.ToString() + " Expose: " + expose.target + " Type: " + expose.type;
            return unit.ToString();
        }
        
        public static readonly string IdPointerNodeIndex = "nodeIndex";
        public static readonly string IdPointerMeshIndex = "meshIndex";
        public static readonly string IdPointerMaterialIndex = "materialIndex";
        public static readonly string IdPointerAnimationIndex = "animationIndex";

        public static void AddPointerConfig(GltfInteractivityNode node, string pointer, string gltfType)
        {
            AddPointerConfig(node, pointer, GltfTypes.TypeIndexByGltfSignature(gltfType));
        }

        public static void AddPointerConfig(GltfInteractivityNode node, string pointer, int gltfType)
        {
            var pointerConfig = node.ConfigurationData[Pointer_SetNode.IdPointer];
            pointerConfig.Value = pointer; 
            var typeConfig = node.ConfigurationData[Pointer_SetNode.IdPointerValueType];
            typeConfig.Value = gltfType; 
        }

        public static bool IsMainCameraInInput(IUnit unit)
        {
            var target = unit.inputs.FirstOrDefault( i => i.key == "target");
            return IsMainCameraInInput(target as ValueInput);
        }
        
        public static bool IsMainCameraInInput(ValueInput target)
        {
            if (target == null)
                return false;
            
            if (!target.hasValidConnection)
                return false;

            var connection = target.connections.First();
            
            if (connection.source != null && connection.source.unit is GetMember getMemberTarget)
            {
                if (getMemberTarget.member.type == typeof(Camera) && getMemberTarget.member.name == nameof(Camera.main))
                    return true;
            }

            return false;
        }
        
        public static void AddPointerTemplateValueInput(GltfInteractivityNode node, string pointerId, int? index = null)
        {
            node.ValueSocketConnectionData.Add(pointerId, new GltfInteractivityNode.ValueSocketData()
            {
                Value = index,
                Type = GltfTypes.TypeIndexByGltfSignature("int"),
                typeRestriction = TypeRestriction.LimitToInt,
            });
        }
        
        // Get the index of a named property that has been exported to GLTF
        public static int GetNamedPropertyGltfIndex(string objectName, IEnumerable<GLTFChildOfRootProperty> gltfRootProperties)
        {
            int i = 0;
            foreach (GLTFChildOfRootProperty property in gltfRootProperties)
            {
                if (objectName == property.Name)
                {
                    return i;
                }
                i++;
            }

            Debug.LogWarning($"Could not find {objectName} in the GLTF List: {gltfRootProperties.ToString()}");
            return -1;
        }

        public static GameObject GetGameObjectFromValueInput(ValueInput value, Dictionary<string, object> defaultValues, VisualScriptingExportContext exportContext)
        {
            if (value.hasValidConnection == false)
            {
                // If there are no connections, then we can return the non-null default value
                if (value.hasDefaultValue && defaultValues.ContainsKey(value.key))
                {
                    GameObject defaultGameObject = null;
                    if (defaultValues[value.key] is GameObject)
                        defaultGameObject = (GameObject)defaultValues[value.key];
                    else if (defaultValues[value.key] is Component component)
                        defaultGameObject = component.gameObject;

                    // If the value is null, then likely it's set to "this" in the UI
                    // meaning that it's pointing to the Gameobject that owns the graph
                    if (defaultGameObject == null)
                    {
                        // The value is referencing the gameobject that owns the graph
                        return exportContext.ActiveScriptMachine.gameObject;
                    }
                    else
                    {
                        return defaultGameObject;
                    }
                }
                return null;
            }
            else
            if (value.hasValidConnection && value.connections.First().source.unit is Literal literal)
            {
                // If there is a connection, then we can return the value of the literal
                if (literal.value is GameObject)
                    return literal.value as GameObject;
                else if (literal.value is Component component)
                    return component.gameObject;

                return null;
            }
            else if (value.hasValidConnection && value.connections.First().source.unit is GetVariable getVariable)
            {
                // If there is a connection, then we can return the value of the literal
                var getVarValue = exportContext.GetVariableValueRaw(getVariable, out _, out var varType);
                if (getVariable != null && getVarValue is GameObject gameObject) 
                    return gameObject;
                else if (getVariable != null && getVarValue is Component component)
                    return component.gameObject;
                
                if (varType != null)
                    return null;
                
                Debug.LogError("Could not get the default value of the GetVariable node: " + getVariable.ToString());
                return null;
            }
            else
            {
                // TODO: Parse the input nodes for a value
                Debug.LogWarning("This ValueInput has a valid connection, but we only support self-references");
                return null;
            }
        }

        public static bool GetDefaultValue<T> (IUnit unit, string key, out T tOut)
        {
            tOut = default(T);
            foreach (ValueInput value in unit.valueInputs)
            {
                if (value.key == key)
                {
                    if (value.type != typeof(T))
                    {
                        Debug.LogWarning($"{key} value key was found in unit {unit.ToString()}, but it is not of type {typeof(T)}, it is {value.type}");
                        return false;
                    }

                    if (value.hasDefaultValue && unit.defaultValues.ContainsKey(value.key))
                    {
                        tOut = (T) unit.defaultValues[value.key];
                        return true;
                        // TODO: Generalize this for the Gameobject edge-case where null means a self-reference
                    }
                    else
                    {
                        Debug.LogWarning($"{key} value key was found in unit {unit.ToString()}, but it has no default values");
                        return false;
                        // TODO: Check if it has a predictable value instead that can be retrieved from static connections.
                    }
                }
            }

            Debug.LogWarning(key + " value key could not be found in the Unit: " + unit.ToString());
            return false;
        }
    }
}
