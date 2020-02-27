using System;
using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class FB_materials_modmapExtension : IExtension
	{
		/// <summary>
		/// A 3-component vector describing the attenuation of the modmap before baseColor application.
		/// </summary>
		public Vector3 ModmapFactor = MODMAP_FACTOR_DEFAULT;
		public static readonly Vector3 MODMAP_FACTOR_DEFAULT = new Vector3(1.0f, 1.0f, 1.0f);

		/// <summary>
		/// The modmap texture.
		/// This texture contains RGB components of the modmap data of the material in sRGB color space.  
		/// </summary>
		public TextureInfo ModmapTexture = MODMAP_TEXTURE_DEFAULT;
		public static readonly TextureInfo MODMAP_TEXTURE_DEFAULT = new TextureInfo();


		public FB_materials_modmapExtension(
			Vector3 modmapFactor,
			TextureInfo modmapTexture)
		{
			ModmapFactor = modmapFactor;
			ModmapTexture = modmapTexture;
		}

		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new FB_materials_modmapExtension(
				ModmapFactor,
				new TextureInfo(
					ModmapTexture,
					gltfRoot
				));
		}

		public JProperty Serialize()
		{
			JObject ext = new JObject();

			if (ModmapFactor != MODMAP_FACTOR_DEFAULT)
			{
				ext.Add(new JProperty(
					FB_materials_modmapExtensionFactory.MODMAP_FACTOR,
					new JArray(ModmapFactor.X, ModmapFactor.Y, ModmapFactor.Z)
				));
			}

			if (ModmapTexture != MODMAP_TEXTURE_DEFAULT)
			{
				ext.Add(new JProperty(
					FB_materials_modmapExtensionFactory.MODMAP_TEXTURE,
						new JObject(
							new JProperty(TextureInfo.INDEX, ModmapTexture.Index.Id)
						)
					)
				);
			}

			return new JProperty(FB_materials_modmapExtensionFactory.EXTENSION_NAME, ext);
		}
	}
}
