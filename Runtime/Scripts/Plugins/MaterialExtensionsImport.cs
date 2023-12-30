namespace UnityGLTF.Plugins
{
    public class MaterialExtensionsImport: GltfImportPlugin
    {
        public bool KHR_materials_ior = true;
        public bool KHR_materials_transmission = true;
        public bool KHR_materials_volume = true;
        public bool KHR_materials_iridescence = true;
        public bool KHR_materials_specular = true;
        public bool KHR_materials_clearcoat = true;
        public bool KHR_materials_pbrSpecularGlossiness = true;
        public bool KHR_materials_emissive_strength = true;
        
        public override string DisplayName => "KHR_materials_* PBR Next Extensions";
        public override string Description => "Import support for various glTF material extensions.";
        public override GltfImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new MaterialExtensionsImportContext(this);
        }
    }

    public class MaterialExtensionsImportContext : GltfImportPluginContext
    {
        internal readonly MaterialExtensionsImport settings;
        
        public MaterialExtensionsImportContext(MaterialExtensionsImport materialExtensionsImport)
        {
            settings = materialExtensionsImport;
        }
    }
}