using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Math;

namespace GLTF.Schema
{
	public class FB_materials_modmapExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "FB_materials_modmap";

		public const string MODMAP_FACTOR = "modmapFactor";
		public const string MODMAP_TEXTURE = "modmapTexture";


		public FB_materials_modmapExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			Vector3 modmapFactor = FB_materials_modmapExtension.MODMAP_FACTOR_DEFAULT;
			TextureInfo modmapTexture = FB_materials_modmapExtension.MODMAP_TEXTURE_DEFAULT;

			if (extensionToken != null)
			{
				JToken modmapFactorToken = extensionToken.Value[MODMAP_FACTOR];
				modmapFactor = modmapFactorToken != null ? modmapFactorToken.DeserializeAsVector3() : modmapFactor;

				JToken modmapTextureToken = extensionToken.Value[MODMAP_TEXTURE];
				modmapTexture = modmapTextureToken != null ? modmapTextureToken.DeserializeAsTexture(root) : modmapTexture;
			}
			
			return new FB_materials_modmapExtension(modmapFactor, modmapTexture);
		}
	}
}
