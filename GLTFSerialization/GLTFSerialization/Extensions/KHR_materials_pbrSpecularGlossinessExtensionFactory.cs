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

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Color diffuseFactor = Color.White;
			TextureInfo diffuseTextureInfo = new TextureInfo();
			Vector3 specularFactor = KHR_materials_pbrSpecularGlossinessExtension.SPEC_FACTOR_DEFAULT;
			double glossinessFactor = KHR_materials_pbrSpecularGlossinessExtension.GLOSS_FACTOR_DEFAULT;
			TextureInfo specularGlossinessTextureInfo = new TextureInfo();

			if (extensionToken != null)
			{
				System.Diagnostics.Debug.WriteLine(extensionToken.Value.ToString());
				System.Diagnostics.Debug.WriteLine(extensionToken.Value.Type);

				JToken diffuseFactorToken = extensionToken.Value[DIFFUSE_FACTOR];
				diffuseFactor = diffuseFactorToken != null ? diffuseFactorToken.DeserializeAsColor() : diffuseFactor;
				diffuseTextureInfo = extensionToken.Value[DIFFUSE_TEXTURE].DeserializeAsTexture(root);
				JToken specularFactorToken = extensionToken.Value[SPECULAR_FACTOR];
				specularFactor = specularFactorToken != null ? specularFactorToken.DeserializeAsVector3() : specularFactor;
				JToken glossinessFactorToken = extensionToken.Value[GLOSSINESS_FACTOR];
				glossinessFactor = glossinessFactorToken != null ? glossinessFactorToken.DeserializeAsDouble() : glossinessFactor;
				specularGlossinessTextureInfo = extensionToken.Value[SPECULAR_GLOSSINESS_TEXTURE].DeserializeAsTexture(root);
			}

			return new KHR_materials_pbrSpecularGlossinessExtension(diffuseFactor, diffuseTextureInfo, specularFactor, glossinessFactor, specularGlossinessTextureInfo);
		}
	}
}
