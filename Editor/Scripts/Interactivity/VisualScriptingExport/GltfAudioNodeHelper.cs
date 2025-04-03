namespace UnityGLTF.Audio
{
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using UnityEngine;
    using Unity.VisualScripting;
    using UnityGLTF.Plugins;
    using UnityGLTF.Interactivity.VisualScripting.Export;
    using UnityGLTF.Interactivity.VisualScripting;

    /// <summary>
    /// Copied the GltfInteractivityNodeHelper for now to use with audio. 
	/// This will need to be cleaned up since a lot of this is not used.
    /// </summary>
    public static class GltfAudioNodeHelper
    {        
        public static Dictionary<IUnit, UnitExporter> GetTranslatableNodes(
            IEnumerable<IUnit> sortedNodes, GLTFAudioExportContext exportContext)
        {
            Dictionary<IUnit, UnitExporter> validNodes =
                new Dictionary<IUnit, UnitExporter>();

            foreach (IUnit unit in sortedNodes)
            {
                if (unit is Literal || unit is This || unit is Null)
                    continue;

                UnitExporter unitExporter = UnitExporterRegistry.CreateUnitExporter(exportContext, unit);
                validNodes.Add(unit, unitExporter);
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
