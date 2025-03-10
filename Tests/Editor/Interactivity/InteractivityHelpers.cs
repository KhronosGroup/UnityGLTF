using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF.Interactivity
{
    internal static class InteractivityHelpers
    {
        [MenuItem("KHR_Interactivity/Log Units with generic inputs")]
        private static void LogMultiInputUnits()
        {
            var multiInputs = TypeCache.GetTypesDerivedFrom<IMultiInputUnit>();

            Debug.Log("Multi-Input Units:\n" + string.Join("\n", multiInputs
                .Where(x => !x.IsGenericType)
                .OrderBy(x =>
                {
                    var category = x.GetAttribute<UnitCategory>();
                    if (category == null) return x.Name;
                    return category.fullName;
                })
                .ThenBy(x =>
                {
                    var title = x.GetAttribute<UnitTitleAttribute>();
                    if (title == null) return x.Name;
                    return title.title;
                })
                .ThenBy(x => x.Name)
                .Select(x =>
                {
                    var category = x.GetAttribute<UnitCategory>();
                    var title = x.GetAttribute<UnitTitleAttribute>();
                    return $"\"{category?.fullName}\" - \"{title?.title}\" ({x.Name})";
                })));
        }
        
        [MenuItem("KHR_Interactivity/Log all Units")]
        private static void LogAllVSLNodes()
        {
            var tree = new UnitOptionTree(new GUIContent("Node"));
            tree.filter = UnitOptionFilter.Any;
            tree.Prewarm();
            var root = tree.Root();
        
            foreach (var node in root)
            {
                Debug.Log(node);
            }
        }
    
        [MenuItem("KHR_Interactivity/Log Units with ValueOutputs of type object")]
        private static void FindAllDynamicValueOutputs()
        {
            var nodes = UnitBase.Subset(
                new UnitOptionFilter(true),
                GraphReference.New(Object.FindObjectOfType<ScriptMachine>(), false));

            nodes = nodes.Where(x => x.valueOutputTypes.Contains(typeof(object)));
            Debug.Log(string.Join('\n', nodes.Select(x => x.haystack).ToArray()));
        }
    }
}
