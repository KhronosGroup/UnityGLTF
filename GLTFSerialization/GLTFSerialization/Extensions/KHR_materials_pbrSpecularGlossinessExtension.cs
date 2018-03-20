using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GLTF.Extensions;

namespace GLTF.Schema
{
	/// <summary>
	/// glTF extension that defines the specular-glossiness 
	/// material model from Physically-Based Rendering (PBR) methodology.
	/// 
	/// Spec can be found here:
	/// https://github.com/KhronosGroup/glTF/tree/master/extensions/Khronos/KHR_materials_pbrSpecularGlossiness
	/// </summary>
	public class KHR_materials_pbrSpecularGlossinessExtension : Extension
	{
		public static readonly Vector3 SPEC_FACTOR_DEFAULT = new Vector3(0.2f, 0.2f, 0.2f);
		public static readonly double GLOSS_FACTOR_DEFAULT = 0.5d; 

		/// <summary>
		/// The RGBA components of the reflected diffuse color of the material. 
		/// Metals have a diffuse value of [0.0, 0.0, 0.0]. 
		/// The fourth component (A) is the alpha coverage of the material. 
		/// The <see cref="Material.AlphaMode"/> property specifies how alpha is interpreted. 
		/// The values are linear.
		/// </summary>
		public Color DiffuseFactor = Color.White;

		/// <summary>
		/// The diffuse texture. 
		/// This texture contains RGB(A) components of the reflected diffuse color of the material in sRGB color space. 
		/// If the fourth component (A) is present, it represents the alpha coverage of the 
		/// material. Otherwise, an alpha of 1.0 is assumed. 
		/// The <see cref="Material.AlphaMode"/> property specifies how alpha is interpreted. 
		/// The stored texels must not be premultiplied.
		/// </summary>
		public TextureInfo DiffuseTexture;

		/// <summary>
		/// The specular RGB color of the material. This value is linear
		/// </summary>
		public Vector3 SpecularFactor = SPEC_FACTOR_DEFAULT;

		/// <summary>
		/// The glossiness or smoothness of the material. 
		/// A value of 1.0 means the material has full glossiness or is perfectly smooth. 
		/// A value of 0.0 means the material has no glossiness or is completely rough. 
		/// This value is linear.
		/// </summary>
		public double GlossinessFactor = GLOSS_FACTOR_DEFAULT;

		/// <summary>
		/// The specular-glossiness texture is RGBA texture, containing the specular color of the material (RGB components) and its glossiness (A component). 
		/// The values are in sRGB space.
		/// </summary>
		public TextureInfo SpecularGlossinessTexture;

		public KHR_materials_pbrSpecularGlossinessExtension(Color diffuseFactor, TextureInfo diffuseTexture, Vector3 specularFactor, double glossinessFactor, TextureInfo specularGlossinessTexture)
		{
			DiffuseFactor = diffuseFactor;
			DiffuseTexture = diffuseTexture;
			SpecularFactor = specularFactor;
			GlossinessFactor = glossinessFactor;
			SpecularGlossinessTexture = specularGlossinessTexture;
		}

		public JProperty Serialize()
		{
			JObject specularGloss = new JObject();
			if (DiffuseFactor != Color.White)
				specularGloss.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.DIFFUSE_FACTOR, DiffuseFactor.asJSONArray());
			if (DiffuseTexture != null)
				specularGloss.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.DIFFUSE_TEXTURE, new JObject(new JProperty(TextureInfo.INDEX, DiffuseTexture.Index.Id)));
			if(SpecularFactor != SPEC_FACTOR_DEFAULT)
				specularGloss.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.SPECULAR_FACTOR, SpecularFactor.asJSONArray());
			if(GlossinessFactor != GLOSS_FACTOR_DEFAULT)
				specularGloss.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.GLOSSINESS_FACTOR, GlossinessFactor);
			if(SpecularGlossinessTexture != null)
				specularGloss.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.SPECULAR_GLOSSINESS_TEXTURE, new JObject(new JProperty(TextureInfo.INDEX, SpecularGlossinessTexture.Index.Id)));

			return new JProperty(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME, specularGloss);
		}
	}
}