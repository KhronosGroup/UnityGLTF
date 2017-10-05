using System;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;

namespace GLTF.Schema
{
    /// <summary>
    /// A set of parameter values that are used to define the specular-glossiness
    /// material model from Physically-Based Rendering (PBR) methodology.
    /// 
    /// Supports the KHR_materials_pbrSpecularGlossiness material extension.
    /// </summary>
    public class PbrSpecularGlossiness : GLTFProperty
    {
        /// <summary>
        /// The RGBA components of the base color of the material.
        /// The fourth component (A) is the opacity of the material.
        /// These values are linear.
        /// </summary>
        public Color BaseColorFactor = Color.White;

        /// <summary>
        /// The base color texture.
        /// This texture contains RGB(A) components in sRGB color space.
        /// The first three components (RGB) specify the base color of the material.
        /// If the fourth component (A) is present, it represents the opacity of the
        /// material. Otherwise, an opacity of 1.0 is assumed.
        /// </summary>
        public TextureInfo BaseColorTexture;

        /// <summary>
        /// The specularity of the material.
        /// A value of 1.0 means the material is shiny and mirror-like.
        /// A value of 0.0 means the material is completely diffuse.
        /// Values in between are for blending between metals and dielectrics such as
        /// plastics.
        /// This value is linear.
        /// </summary>
        public double SpecularFactor = 1;

        /// <summary>
        /// The glossiness of the material.
        /// A value of 1.0 means the material is completely rough.
        /// A value of 0.0 means the material is completely smooth.
        /// This value is linear.
        /// </summary>
        public double GlossinessFactor = 1;

        /// <summary>
        /// The specular-glossiness texture has two components.
        /// The first component (R) contains the specularity of the material.
        /// The second component (G) contains the glossiness of the material.
        /// These values are linear.
        /// If the third component (B) and/or the fourth component (A) are present,
        /// they are ignored.
        /// </summary>
        public TextureInfo SpecularGlossinessTexture;

        public static PbrSpecularGlossiness Deserialize(GLTFRoot root, JsonReader reader)
        {
            var specularGlossiness = new PbrSpecularGlossiness();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "diffuseFactor":
                        specularGlossiness.BaseColorFactor = reader.ReadAsRGBAColor();
                        break;
                    case "diffuseTexture":
                        specularGlossiness.BaseColorTexture = TextureInfo.Deserialize(root, reader);
                        break;
                    case "specularFactor":
                        specularGlossiness.SpecularFactor = reader.ReadAsDouble().Value;
                        break;
                    case "glossinessFactor":
                        specularGlossiness.GlossinessFactor = reader.ReadAsDouble().Value;
                        break;
                    case "specularGlossinessTexture":
                        specularGlossiness.SpecularGlossinessTexture = TextureInfo.Deserialize(root, reader);
                        break;
                    default:
                        specularGlossiness.DefaultPropertyDeserializer(root, reader);
                        break;
                }
            }

            return specularGlossiness;
        }

        public override void Serialize(JsonWriter writer)
        {
            writer.WriteStartObject();

            if (BaseColorFactor != Color.White)
            {
                writer.WritePropertyName("diffuseFactor");
                writer.WriteStartArray();
                writer.WriteValue(BaseColorFactor.R);
                writer.WriteValue(BaseColorFactor.G);
                writer.WriteValue(BaseColorFactor.B);
                writer.WriteValue(BaseColorFactor.A);
                writer.WriteEndArray();
            }

            if (BaseColorTexture != null)
            {
                writer.WritePropertyName("diffuseTexture");
                BaseColorTexture.Serialize(writer);
            }

            if (SpecularFactor != 1.0f)
            {
                writer.WritePropertyName("specularFactor");
                writer.WriteValue(SpecularFactor);
            }

            if (GlossinessFactor != 1.0f)
            {
                writer.WritePropertyName("glossinessFactor");
                writer.WriteValue(GlossinessFactor);
            }

            if (SpecularGlossinessTexture != null)
            {
                writer.WritePropertyName("specularGlossinessTexture");
                SpecularGlossinessTexture.Serialize(writer);
            }

            base.Serialize(writer);

            writer.WriteEndObject();
        }
    }
}
