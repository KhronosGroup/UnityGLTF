using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityGLTF.Interactivity;
using UnityGLTF.Interactivity.Export;
using Object = UnityEngine.Object;

namespace Editor.UnitExporters.Lists
{
    public class FindObjectsOfTypeUnitExport : IUnitExporter, IUnitExporterFeedback
    {
        public Type unitType
        {
            get => typeof(InvokeMember);
        }
        
        [InitializeOnLoadMethod]
        private static void Register()
        {
            InvokeUnitExport.RegisterInvokeExporter(typeof(UnityEngine.Object), nameof(UnityEngine.Object.FindObjectsByType), new FindObjectsOfTypeUnitExport());
        }
        
        public bool InitializeInteractivityNodes(UnitExporter unitExporter)
        {
            var unit = unitExporter.unit as InvokeMember;

            if (!GltfInteractivityNodeHelper.GetDefaultValue(unit, "%type", out Type type))
                return false;
            if (!GltfInteractivityNodeHelper.GetDefaultValue(unit, "%sortMode", out FindObjectsSortMode sortMode)) 
                return false;

            var objects = Object.FindObjectsByType(type, FindObjectsSortMode.None);
            var transforms = objects.Select(obj =>
            {
                if (obj is Transform transform)
                    return transform;
                if (obj is GameObject gameObject)
                    return gameObject.transform;
                if (obj is Component component)
                    return component.transform;
                return null;
            }).Where( obj => obj != null).ToArray();

            var transformsIndicies = transforms
                .Select(transform => unitExporter.exportContext.exporter.GetTransformIndex(transform)).Where(trIndex => trIndex != -1);
            
            var objectList = unitExporter.exportContext.CreateNewVariableBasedListFromUnit(unit, transformsIndicies.Count(),
                GltfTypes.TypeIndexByGltfSignature("int"));
            
            foreach (var transformIndex in transformsIndicies)
                objectList.AddItem(transformIndex);

            objectList.listCreatorGraph = unitExporter.exportContext.currentGraphProcessing;
            
            ListHelpers.CreateListNodes(unitExporter, objectList);
            
            
            unitExporter.ByPassFlow(unit.enter, unit.exit);
            return true;
        }
        
        public UnitLogs GetFeedback(IUnit unit)
        {
            var logs = new UnitLogs();
            logs.infos.Add("This will be exported as a static list of indices of the found objects.");
            
            return logs;
        }
    }
}