using UnityEngine;

namespace UnityGLTF.Plugins
{
    public class MaterialExtensionsImport: GLTFImportPlugin
    {
        public bool KHR_materials_ior = true;
        public bool KHR_materials_transmission = true;
        public bool KHR_materials_volume = true;
        public bool KHR_materials_iridescence = true;
        public bool KHR_materials_specular = true;
        public bool KHR_materials_clearcoat = true;
        public bool KHR_materials_sheen = true;
        [HideInInspector] // legacy
        public bool KHR_materials_pbrSpecularGlossiness = true;
        public bool KHR_materials_emissive_strength = true;
        public bool KHR_materials_anisotropy = true;
        
        public override string DisplayName => "KHR_materials_* PBR Next Extensions";
        public override string Description => "Import support for various glTF material extensions.";
        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new MaterialExtensionsImportContext(this);
        }
    }

    public class MaterialExtensionsImportContext : GLTFImportPluginContext
    {
        internal readonly MaterialExtensionsImport settings;
        
        public MaterialExtensionsImportContext(MaterialExtensionsImport materialExtensionsImport)
        {
            settings = materialExtensionsImport;
        }
    }
}