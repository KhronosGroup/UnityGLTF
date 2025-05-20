using System;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    // When a plugin is registered with the default settings (the scriptable object in the project),
    // it will be active "by default" when someone uses those default settings.
    // e.g. it's used when someone uses the built-in editor methods for exporting objects.
    // When using the API, one needs to manually register wanted plugins and configure them
    // (can get the default settings and modify them).
	
    // Plugins can contain any number of extensions, but are encouraged to specify in the description
    // which extensions are imported/exported with that plugin.
    // Theoretically there could be multiple plugins operating on the same extension in different ways, in
    // which case we currently can't warn about conflicts; they would all run.
    // If plugins were required to list the extensions they operate on, we could warn about conflicts.
	
    // Plugins are ScriptableObjects which are added to the default GLTFSettings scriptable object.
    // Their public serialized fields are exposed in the inspector, and they can be enabled/disabled.
    // Plugins replace both GLTFSceneExporter.* static callbacks and GLTFSceneExporter.ExportOptions callbacks
    // to allow for more control.
	
    // Example cases where separate plugins operate on the same data:
    // - exporting UI as custom extension vs. baking UI to mesh
    // - exporting Audio in a custom extension vs. using KHR_audio
    // - exporting LODs as custom extension vs. using MSFT_lod
    // - exporting particle systems as custom extension vs. baking to mesh
	
    // Plugins can either be added manually to ExportOptions.plugins / ImportContext.plugins
    // or advertise themselves via a static callback which allows configuring their settings in the inspector.
    // For each new instance of GLTFSceneExporter, new instances of plugins are created.
    // For each new instance of GLTFSceneImporter, new instances of plugins are created.
    
    public abstract class GLTFPlugin: ScriptableObject
    {
        public abstract string DisplayName { get; }
        public virtual string Description => null;
        public virtual string HelpUrl => null;
        
        [SerializeField] [HideInInspector] private bool enabled = true;
        public virtual bool Enabled
        {
	        get
	        {
		        return enabled || AlwaysEnabled; 
	        }
	        set
	        {
		        enabled = value;
	        }
        }
        public virtual bool EnabledByDefault => true;
        public virtual bool AlwaysEnabled => false;
        public virtual string Warning => null;
        
        public virtual bool PackageMissing => false;
        
        [Obsolete("Use a custom Editor for this ScriptableObject instead if you want to override how it looks in the Inspector.")]
        public virtual void OnGUI() { }
    }
	
    [Obsolete("Use UnityGLTF.Plugins.GLTFPlugin instead. (UnityUpgradable) -> UnityGLTF.Plugins.GLTFPlugin")]
    public abstract class GltfPlugin: ScriptableObject {}
    
    [Obsolete("Use UnityGLTF.Plugins.GLTFImportPlugin instead. (UnityUpgradable) -> UnityGLTF.Plugins.GLTFImportPlugin")]
    public abstract class GltfImportPlugin: GltfPlugin {}
    
    [Obsolete("Use UnityGLTF.Plugins.GLTFExportPlugin instead. (UnityUpgradable) -> UnityGLTF.Plugins.GLTFExportPlugin")]
    public abstract class GltfExportPlugin: GltfPlugin {}
    
    [Obsolete("Use UnityGLTF.Plugins.GLTFImportPluginContext instead. (UnityUpgradable) -> UnityGLTF.Plugins.GLTFImportPluginContext")]
    public abstract class GltfImportPluginContext {}
    
    [Obsolete("Use UnityGLTF.Plugins.GLTFExportPluginContext instead. (UnityUpgradable) -> UnityGLTF.Plugins.GLTFExportPluginContext")]
    public abstract class GltfExportPluginContext {}
}