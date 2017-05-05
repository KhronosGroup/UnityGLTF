using System;
using GLTF;
using UnityEngine;
using Newtonsoft.Json;
using GLTF.JsonExtensions;

namespace GLTF.Extensions
{
    class KHRMaterialPBRSpecularGlossiness : GLTFExtension
    {
        public static string ExtensionName = "KHR_materials_pbrSpecularGlossiness";
        
        /// <summary>
        /// The RGBA components of the reflected diffuse color of the material.
        /// Metals have a diffuse value of `[0.0, 0.0, 0.0]`.
        /// The fourth component (A) is the alpha coverage of the material.
        /// The `alphaMode` property specifies how alpha is interpreted. The values are linear.
        /// </summary>
        public Color DiffuseFactor = Color.white;

        public GLTFTextureInfo DiffuseTexture;

        public double GlossinessFactor = 1.0;

        public Color SpecularFactor = Color.white;

        public GLTFTextureInfo SpecularGlossinessTexture;


        public void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();

            if (DiffuseFactor != Color.white)
            {
                writer.WritePropertyName("diffuseFactor");
                writer.WriteValue(DiffuseFactor.r);
                writer.WriteValue(DiffuseFactor.g);
                writer.WriteValue(DiffuseFactor.b);
                writer.WriteValue(DiffuseFactor.a);
            }

            if (DiffuseTexture != null)
            {
                writer.WritePropertyName("diffuseTexture");
                DiffuseTexture.Serialize(writer);
            }

            if (GlossinessFactor != 1.0f)
            {
                writer.WritePropertyName("glossinessFactor");
                writer.WriteValue(GlossinessFactor);
            }

            if (SpecularFactor != Color.white)
            {
                writer.WritePropertyName("specularFactor");
                writer.WriteValue(SpecularFactor.r);
                writer.WriteValue(SpecularFactor.g);
                writer.WriteValue(SpecularFactor.b);
            }

            if (SpecularGlossinessTexture != null)
            {
                writer.WritePropertyName("specularGlossinessTexture");
                SpecularGlossinessTexture.Serialize(writer);
            }
            
            writer.WriteEndObject();
        }
    }

    class KHRMaterialPBRSpecularGlossinessFactory : GLTFExtensionFactory
    {
        public new string ExtensionName = "KHR_materials_pbrSpecularGlossiness";
        public override GLTFExtension Deserialize(GLTFRoot root, JsonReader reader)
        {
            var specGloss = new KHRMaterialPBRSpecularGlossiness();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Extension must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "diffuseFactor":
                        specGloss.DiffuseFactor = reader.ReadAsRGBAColor();
                        break;
                    case "baseColorTexture":
                        specGloss.DiffuseTexture = GLTFTextureInfo.Deserialize(root, reader);
                        break;
                    case "glossinessFactor":
                        specGloss.GlossinessFactor = reader.ReadAsDouble().Value;
                        break;
                    case "specularFactor":
                        specGloss.SpecularFactor = reader.ReadAsRGBColor();
                        break;
                    case "specularGlossinessTexture":
                        specGloss.SpecularGlossinessTexture = GLTFTextureInfo.Deserialize(root, reader);
                        break;
					default:
						break;
				}
            }

            return specGloss;
        }
    }
}