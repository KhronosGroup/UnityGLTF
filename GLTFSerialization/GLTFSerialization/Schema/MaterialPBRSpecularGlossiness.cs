using System;
using GLTF.Extensions;
using GLTF.Math;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GLTF.Schema
{
    // TODO: move this code to KHR_materials_pbrSpecularGlossinessExtension

    /// <summary>
    /// glTF extension that defines the specular-glossiness 
    /// material model from Physically-Based Rendering (PBR) methodology.
    /// 
    /// Spec can be found here:
    /// https://github.com/KhronosGroup/glTF/tree/master/extensions/Khronos/KHR_materials_pbrSpecularGlossiness
    /// </summary>
    public class PbrSpecularGlossiness : GLTFProperty
    {
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
        public Vector3 SpecularFactor = Vector3.One;

        /// <summary>
        /// The glossiness or smoothness of the material. 
        /// A value of 1.0 means the material has full glossiness or is perfectly smooth. 
        /// A value of 0.0 means the material has no glossiness or is completely rough. 
        /// This value is linear.
        /// </summary>
        public double GlossinessFactor = 1;

        /// <summary>
        /// The specular-glossiness texture is RGBA texture, containing the specular color of the material (RGB components) and its glossiness (A component). 
        /// The values are in sRGB space.
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
                        specularGlossiness.DiffuseFactor = reader.ReadAsRGBAColor();
                        break;
                    case "diffuseTexture":
                        specularGlossiness.DiffuseTexture = TextureInfo.Deserialize(root, reader);
                        break;
                    case "specularFactor":
                        specularGlossiness.SpecularFactor = reader.ReadAsVector3();
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

            if (DiffuseFactor != Color.White)
            {
                writer.WritePropertyName("diffuseFactor");
                writer.WriteStartArray();
                writer.WriteValue(DiffuseFactor.R);
                writer.WriteValue(DiffuseFactor.G);
                writer.WriteValue(DiffuseFactor.B);
                writer.WriteValue(DiffuseFactor.A);
                writer.WriteEndArray();
            }

            if (DiffuseTexture != null)
            {
                writer.WritePropertyName("diffuseTexture");
                DiffuseTexture.Serialize(writer);
            }

            if (SpecularFactor != Vector3.One)
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
