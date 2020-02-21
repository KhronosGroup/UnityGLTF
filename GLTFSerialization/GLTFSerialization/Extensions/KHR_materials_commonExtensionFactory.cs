using Newtonsoft.Json.Linq;
using GLTF.Extensions;
using GLTF.Math;
using System;

namespace GLTF.Schema
{
	public class KHR_materials_commonExtensionFactory : ExtensionFactory
	{
		public const string EXTENSION_NAME = "KHR_materials_common";

		public const string TECHNIQUE = "technique";
		public const string VALUES = "values";
		public const string AMBIENT = "ambient";
		public const string EMISSION = "emission";
		public const string DIFFUSE = "diffuse";
		public const string SPECULAR = "specular";
		public const string SHININESS = "shininess";
		public const string TRANSPARENCY = "transparency";
		public const string TRANSPARENT = "transparent";
		public const string DOUBLESIDED = "doublesided";


		public KHR_materials_commonExtensionFactory()
		{
			ExtensionName = EXTENSION_NAME;
		}

		public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
		{
			KHR_materials_commonExtension.CommonTechnique technique = KHR_materials_commonExtension.TECHNIQUE_DEFAULT;
			Color ambient = KHR_materials_commonExtension.AMBIENT_DEFAULT;
			Color emissionColor = KHR_materials_commonExtension.EMISSIONCOLOR_DEFAULT;
			TextureInfo emissionTexture = new TextureInfo();
			Color diffuseColor = KHR_materials_commonExtension.DIFFUSECOLOR_DEFAULT;
			TextureInfo diffuseTexture = new TextureInfo();
			Color specularColor = KHR_materials_commonExtension.SPECULARCOLOR_DEFAULT;
			TextureInfo specularTexture = new TextureInfo();
			float shininess = KHR_materials_commonExtension.SHININESS_DEFAULT;
			float transparency = KHR_materials_commonExtension.TRANSPARENCY_DEFAULT;
			bool transparent = KHR_materials_commonExtension.TRANSPARENT_DEFAULT;
			bool doubleSided = KHR_materials_commonExtension.DOUBLESIDED_DEFAULT;

			if (extensionToken != null)
			{
				JToken techniqueToken = extensionToken.Value[TECHNIQUE];
				if(techniqueToken != null)
				{
					switch(techniqueToken.DeserializeAsString())
					{
						case "CONSTANT":
							technique = KHR_materials_commonExtension.CommonTechnique.CONSTANT;
							break;
						case "LAMBERT":
							technique = KHR_materials_commonExtension.CommonTechnique.LAMBERT;
							break;
						case "PHONG":
							technique = KHR_materials_commonExtension.CommonTechnique.PHONG;
							break;
						case "BLINN":
							technique = KHR_materials_commonExtension.CommonTechnique.BLINN;
							break;
						default:
							throw new Exception("Invalid technique type: " + techniqueToken.DeserializeAsString());
					}
				}

				JToken valuesToken = extensionToken.Value[VALUES];
				if(valuesToken != null)
				{
					JObject valuesObject = valuesToken as JObject;
					if (valuesObject != null)
					{
						JToken ambientToken = valuesObject[AMBIENT];
						ambient = ambientToken != null ? ambientToken.DeserializeAsColor() : ambient;

						JToken emissionToken = valuesObject[EMISSION];
						if (emissionToken != null)
						{
							if (emissionToken.Type == JTokenType.Integer)
							{
								emissionTexture = emissionToken.DeserializeAsTexture(root);
							}
							else if(emissionToken.Type == JTokenType.Array)
							{
								emissionColor = emissionToken.DeserializeAsColor();
							}
						}

						if (technique == KHR_materials_commonExtension.CommonTechnique.LAMBERT ||
							technique == KHR_materials_commonExtension.CommonTechnique.PHONG ||
							technique == KHR_materials_commonExtension.CommonTechnique.BLINN ||
							technique == KHR_materials_commonExtension.CommonTechnique.CONSTANT)
						{
							JToken diffuseToken = valuesObject[DIFFUSE];
							if (diffuseToken != null)
							{
								if (diffuseToken.Type == JTokenType.Integer)
								{
									diffuseTexture = diffuseToken.DeserializeAsTexture(root);
								}
								else if (diffuseToken.Type == JTokenType.Array)
								{
									diffuseColor = diffuseToken.DeserializeAsColor();
								}
							}
						}

						if (technique == KHR_materials_commonExtension.CommonTechnique.PHONG ||
							technique == KHR_materials_commonExtension.CommonTechnique.BLINN)
						{
							JToken specularToken = valuesObject[SPECULAR];
							if (specularToken != null)
							{
								if (specularToken.Type == JTokenType.Integer)
								{
									diffuseTexture = specularToken.DeserializeAsTexture(root);
								}
								else if (specularToken.Type == JTokenType.Array)
								{
									diffuseColor = specularToken.DeserializeAsColor();
								}
							}
						}

						JToken shininessToken = valuesObject[SHININESS];
						shininess = shininessToken != null ? shininessToken.DeserializeAsFloat() : shininess;

						JToken transparencyToken = valuesObject[TRANSPARENCY];
						transparency = transparencyToken != null ? transparencyToken.DeserializeAsFloat() : transparency;

						JToken transparentToken = valuesObject[TRANSPARENT];
						transparent = transparentToken != null ? transparentToken.DeserializeAsBool() : transparent;

						JToken doubleSidedToken = valuesObject[DOUBLESIDED];
						doubleSided = doubleSidedToken != null ? doubleSidedToken.DeserializeAsBool() : doubleSided;
					}
				}
			}
			
			return new KHR_materials_commonExtension(technique,
				ambient,
				emissionColor,
				emissionTexture,
				diffuseColor,
				diffuseTexture,
				specularColor,
				specularTexture,
				shininess,
				transparency,
				transparent,
				doubleSided);
		}
	}
}
