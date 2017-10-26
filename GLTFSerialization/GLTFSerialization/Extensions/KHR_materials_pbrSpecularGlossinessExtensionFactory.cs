using System;
using Newtonsoft.Json.Linq;
using GLTF.Math;
using Newtonsoft.Json;
using GLTF.Extensions;

namespace GLTF.Schema
{
    public class KHR_materials_pbrSpecularGlossinessExtensionFactory : ExtensionFactory
    {
        public const string EXTENSION_NAME = "KHR_materials_pbrSpecularGlossiness";
        public const string DIFFUSE_FACTOR = "diffuseFactor";
        public const string DIFFUSE_TEXTURE = "diffuseTexture";
        public const string SPECULAR_FACTOR = "specularFactor";
        public const string GLOSSINESS_FACTOR = "glossinessFactor";
        public const string SPECULAR_GLOSSINESS_TEXTURE = "specularGlossinessTexture";

        public KHR_materials_pbrSpecularGlossinessExtensionFactory()
        {
            ExtensionName = EXTENSION_NAME;
        }

        public override Extension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            Color diffuseFactor = new Color();
            TextureInfo diffuseTextureInfo = new TextureInfo();
            Vector3 specularFactor = new Vector3();
            double glossinessFactor = 0f;
            TextureInfo specularGlossinessTextureInfo = new TextureInfo();

            if (extensionToken != null)
            {
                System.Diagnostics.Debug.WriteLine(extensionToken.Value.ToString());
                System.Diagnostics.Debug.WriteLine(extensionToken.Value.Type);

                diffuseFactor = extensionToken.Value[DIFFUSE_FACTOR].DeserializeAsColor();
                diffuseTextureInfo = extensionToken.Value[DIFFUSE_TEXTURE].DeserializeAsTexture(root);
                specularFactor = extensionToken.Value[SPECULAR_FACTOR].DeserializeAsVector3();
                glossinessFactor = extensionToken.Value[GLOSSINESS_FACTOR].DeserializeAsDouble();
                specularGlossinessTextureInfo = extensionToken.Value[SPECULAR_GLOSSINESS_TEXTURE].DeserializeAsTexture(root);
            }

            return new KHR_materials_pbrSpecularGlossinessExtension(diffuseFactor, diffuseTextureInfo, specularFactor, glossinessFactor, specularGlossinessTextureInfo);
        }
    }
}