using System;
using GLTF.Math;
using GLTF.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GLTF.Schema
{
	public class KHR_materials_commonExtension : IExtension
	{
		public enum CommonTechnique
		{
			NONE = -1,
			CONSTANT,
			LAMBERT,
			PHONG,
			BLINN
		}

		/// <summary>
		/// Specifies the shading technique used e.g.BLINN, and values which contains a set of technique-specific values.
		/// </summary>
		public CommonTechnique Technique = CommonTechnique.CONSTANT;
		public static readonly CommonTechnique TECHNIQUE_DEFAULT = CommonTechnique.NONE;

		/// <summary>
		/// RGBA value for ambient light reflected from the surface of the object.
		/// </summary>
		public Color Ambient = AMBIENT_DEFAULT;
		public static readonly Color AMBIENT_DEFAULT = Color.Black;

		/// <summary>
		/// RGBA value for light emitted by the surface of the object.
		/// </summary>
		public Color EmissionColor = EMISSIONCOLOR_DEFAULT;
		public static readonly Color EMISSIONCOLOR_DEFAULT = Color.Black;

		/// <summary>
		/// Texture for light emitted by the surface of the object.
		/// </summary>
		public TextureInfo EmissionTexture = EMISSIONTEXTURE_DEFAULT;
		public static readonly TextureInfo EMISSIONTEXTURE_DEFAULT = new TextureInfo();

		/// <summary>
		/// RGBA value defining the amount of light diffusely reflected from the surface of the object.
		/// </summary>
		public Color DiffuseColor = DIFFUSECOLOR_DEFAULT;
		public static readonly Color DIFFUSECOLOR_DEFAULT = Color.Black;

		/// <summary>
		/// Texture defining the amount of light diffusely reflected from the surface of the object.
		/// </summary>
		public TextureInfo DiffuseTexture = DIFFUSETEXTURE_DEFAULT;
		public static readonly TextureInfo DIFFUSETEXTURE_DEFAULT = new TextureInfo();

		/// <summary>
		/// RGBA value defining the color of light specularly reflected from the surface of the object.
		/// </summary>
		public Color SpecularColor = SPECULARCOLOR_DEFAULT;
		public static readonly Color SPECULARCOLOR_DEFAULT = Color.Black;

		/// <summary>
		/// Texture defining the color of light specularly reflected from the surface of the object.
		/// </summary>
		public TextureInfo SpecularTexture = SPECULARTEXTURE_DEFAULT;
		public static readonly TextureInfo SPECULARTEXTURE_DEFAULT = new TextureInfo();

		/// <summary>
		/// Defines the specularity or roughness of the specular reflection lobe of the object.
		/// </summary>
		public float Shininess = SHININESS_DEFAULT;
		public static readonly float SHININESS_DEFAULT = 0.0f;

		/// <summary>
		/// Declares the amount of transparency as an opacity value between 0.0 and 1.0.
		/// </summary>
		public float Transparency = TRANSPARENCY_DEFAULT;
		public static readonly float TRANSPARENCY_DEFAULT = 1.0f;

		/// <summary>
		/// Declares whether the visual should be rendered using alpha blending.
		/// Corresponds to enabling the BLEND render state, setting the depthMask property to false,
		/// and defining blend equations and blend functions as described in the implementation note.
		/// </summary>
		public bool Transparent = TRANSPARENT_DEFAULT;
		public static readonly bool TRANSPARENT_DEFAULT = false;

		/// <summary>
		/// Declares whether backface culling should be disabled for this visual. Corresponds to disabling the CULL_FACE render state.
		/// </summary>
		public bool DoubleSided = DOUBLESIDED_DEFAULT;
		public static readonly bool DOUBLESIDED_DEFAULT = false;


		public KHR_materials_commonExtension(
			CommonTechnique technique,
			Color ambient,
			Color emissionColor,
			TextureInfo emissionTexture,
			Color diffuseColor,
			TextureInfo diffuseTexture,
			Color specularColor,
			TextureInfo specularTexture,
			float shininess,
			float transparency,
			bool transparent,
			bool doubleSided)
		{
			Technique = technique;
			Ambient = ambient;
			EmissionColor = emissionColor;
			EmissionTexture = emissionTexture;
			DiffuseColor = diffuseColor;
			DiffuseTexture = diffuseTexture;
			SpecularColor = specularColor;
			SpecularTexture = specularTexture;
			Shininess = shininess;
			Transparency = transparency;
			Transparent = transparent;
			DoubleSided = doubleSided;
		}

		public IExtension Clone(GLTFRoot gltfRoot)
		{
			return new KHR_materials_commonExtension(
				Technique, Ambient, EmissionColor,
				new TextureInfo(
					EmissionTexture,
					gltfRoot
				),
				DiffuseColor,
				new TextureInfo(
					DiffuseTexture,
					gltfRoot
				),
				SpecularColor,
				new TextureInfo(
					SpecularTexture,
					gltfRoot
				),
				Shininess, Transparency, Transparent, DoubleSided);
		}

		public JProperty Serialize()
		{
			if(CommonTechnique.NONE == Technique)
			{
				return new JProperty("", new JObject());
			}

			JObject ext = new JObject();

			ext.Add(new JProperty(
				KHR_materials_commonExtensionFactory.TECHNIQUE,
				Technique.ToString()
			));

			var valuesObj = new JObject();
			ext.Add(new JProperty(
				KHR_materials_commonExtensionFactory.VALUES,
				valuesObj)
			);

			if (Ambient != AMBIENT_DEFAULT)
			{
				valuesObj.Add(new JProperty(
					KHR_materials_commonExtensionFactory.AMBIENT,
					new JArray(Ambient.R, Ambient.G, Ambient.B)
				));
			}

			if (EmissionTexture != EMISSIONTEXTURE_DEFAULT)
			{
				valuesObj.Add(new JProperty(
					KHR_materials_commonExtensionFactory.EMISSION,
						new JObject(
							new JProperty(TextureInfo.INDEX, EmissionTexture.Index.Id)
						)
					)
				);
			}
			else
			{
				if (EmissionColor != EMISSIONCOLOR_DEFAULT)
				{
					valuesObj.Add(new JProperty(
						KHR_materials_commonExtensionFactory.EMISSION,
						new JArray(EmissionColor.R, EmissionColor.G, EmissionColor.B)
					));
				}
			}

			if (Technique == CommonTechnique.LAMBERT ||
				Technique == CommonTechnique.PHONG ||
				Technique == CommonTechnique.BLINN ||
				Technique == CommonTechnique.CONSTANT)
			{
				if (DiffuseTexture != DIFFUSETEXTURE_DEFAULT)
				{
					valuesObj.Add(new JProperty(
						KHR_materials_commonExtensionFactory.DIFFUSE,
							new JObject(
								new JProperty(TextureInfo.INDEX, DiffuseTexture.Index.Id)
							)
						)
					);
				}
				else
				{
					if (DiffuseColor != DIFFUSECOLOR_DEFAULT)
					{
						valuesObj.Add(new JProperty(
							KHR_materials_commonExtensionFactory.DIFFUSE,
							new JArray(DiffuseColor.R, DiffuseColor.G, DiffuseColor.B)
						));
					}
				}
			}

			if (Technique == CommonTechnique.PHONG ||
				Technique == CommonTechnique.BLINN)
			{
				if (SpecularTexture != SPECULARTEXTURE_DEFAULT)
				{
					valuesObj.Add(new JProperty(
						KHR_materials_commonExtensionFactory.SPECULAR,
							new JObject(
								new JProperty(TextureInfo.INDEX, SpecularTexture.Index.Id)
							)
						)
					);
				}
				else
				{
					if (SpecularColor != Color.Black)
					{
						if (SpecularColor != SPECULARCOLOR_DEFAULT)
						{
							valuesObj.Add(new JProperty(
								KHR_materials_commonExtensionFactory.SPECULAR,
								new JArray(SpecularColor.R, SpecularColor.G, SpecularColor.B)
							));
						}
					}
				}

				if (Shininess != SHININESS_DEFAULT)
				{
					valuesObj.Add(new JProperty(
						KHR_materials_commonExtensionFactory.SHININESS,
						Shininess
					));
				}
			}

			if (Transparency != TRANSPARENCY_DEFAULT)
			{
				valuesObj.Add(new JProperty(
					KHR_materials_commonExtensionFactory.TRANSPARENCY,
					Transparency
				));
			}

			if (Transparent != TRANSPARENT_DEFAULT)
			{
				valuesObj.Add(new JProperty(
					KHR_materials_commonExtensionFactory.TRANSPARENT,
					Transparent
				));
			}

			if (DoubleSided != DOUBLESIDED_DEFAULT)
			{
				valuesObj.Add(new JProperty(
					KHR_materials_commonExtensionFactory.DOUBLESIDED,
					DoubleSided
				));	
			}

			return new JProperty(KHR_materials_commonExtensionFactory.EXTENSION_NAME, ext);
		}
	}
}
