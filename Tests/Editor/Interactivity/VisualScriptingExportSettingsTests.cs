using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityGLTF.Interactivity.VisualScripting;

namespace UnityGLTF.Interactivity
{
    /// <summary>
    /// The Visual Scripting export plugin forces KHR_node_visibility, KHR_animation_pointer and "Export Disabled GameObjects"
    /// only when the exported objects have Script Machines. It used to force them for every export.
    /// </summary>
    public class VisualScriptingExportSettingsTests
    {
        /// <summary>
        /// Creates an exporter for a GameObject and returns whether it exports disabled GameObjects, which the settings turn off.
        /// </summary>
        private static bool ExportsDisabledGameObjects(bool withScriptMachine)
        {
            var gameObject = new GameObject(nameof(VisualScriptingExportSettingsTests));
            var settings = ScriptableObject.CreateInstance<GLTFSettings>();
            try
            {
                Assert.IsTrue(settings.ExportPlugins.Any(p => p is VisualScriptingExportPlugin && p.Enabled), "The Visual Scripting export plugin should be enabled by default");
                settings.ExportDisabledGameObjects = false;
                if (withScriptMachine)
                    gameObject.AddComponent<ScriptMachine>().nest.SwitchToEmbed(new FlowGraph());

                var exporter = new GLTFSceneExporter(gameObject.transform, new ExportContext(settings));
                return exporter.ExportDisabledGameObjects == true;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void WithScriptMachine_ForcesInteractivitySettings()
        {
            Assert.IsTrue(ExportsDisabledGameObjects(true), "Exports with a Script Machine need disabled GameObjects (graphs can enable them)");
        }

        [Test]
        public void WithoutScriptMachine_KeepsExportSettings()
        {
            Assert.IsFalse(ExportsDisabledGameObjects(false), "Exports without Script Machines should keep the export settings");
        }
    }
}
