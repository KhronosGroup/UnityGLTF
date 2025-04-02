using UnityGLTF.Interactivity.Schema;
using UnityGLTF.Interactivity.VisualScripting.Export;

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
            IEnumerable<IUnit> nodes, VisualScriptingExportContext exportContext)
        {
            Dictionary<IUnit, UnitExporter> exporters =
                new Dictionary<IUnit, UnitExporter>();

            foreach (IUnit unit in nodes)
            {
                if (unit is Literal || unit is This || unit is Null)
                    continue;
                
                UnitExporter unitExporter = UnitExporterRegistry.CreateUnitExporter(exportContext, unit);
                if (unitExporter == null)
                {
                    UnitExportLogging.AddErrorLog(unit, "Export not supported");
                    continue;
                }
                exporters.Add(unit, unitExporter);
            }

            return exporters;
        }

        public static string UnitToString(IUnit unit)
        {
            if (unit is InvokeMember invokeMember)
                return unit.ToString() + " Invoke: " + invokeMember.member.declaringType + "." + invokeMember.member.name;
            if (unit is SetMember setMember)
                return unit.ToString() + " Set: " + setMember.member.declaringType + "." + setMember.member.name;
            if (unit is GetMember getMember)
                return unit.ToString() + " Get: " + getMember.member.declaringType + "." + getMember.member.name;
            if (unit is Expose expose)
                return unit.ToString() + " Expose: " + expose.type;
            return unit.ToString();
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
        
        public static GameObject GetGameObjectFromValueInput(ValueInput value, Dictionary<string, object> defaultValues, VisualScriptingExportContext exportContext)
        {
            if (value.hasValidConnection && value.connections.First().source.unit is GraphInput graphInput)
            {
                var subGraphUnit = exportContext.currentGraphProcessing.subGraphUnit;
                if (subGraphUnit != null)
                {
                    var graphValueKey = value.connections.First().source.key;
                    // Reroute to the SubGraph Unit to get the value
                    value = subGraphUnit.valueInputs[graphValueKey];
                }
            }
            
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
            else
            if (value.hasValidConnection && value.connections.First().source.unit is This thisUnit)
            {
                return exportContext.ActiveScriptMachine.gameObject;
            }
            else if (value.hasValidConnection && value.connections.First().source.unit is GetVariable getVariable)
            {
                // If there is a connection, then we can return the value of the literal
                var getVarValue = exportContext.GetVariableValueRaw(getVariable, out _, out var cSharpVarType);
                if (getVariable != null && getVarValue is GameObject gameObject) 
                    return gameObject;
                else if (getVariable != null && getVarValue is Component component)
                    return component.gameObject;
                
                if (cSharpVarType != null)
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
