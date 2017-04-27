using System;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// A set of parameter values that are used to define the metallic-roughness
    /// material model from Physically-Based Rendering (PBR) methodology.
    /// </summary>
    public class GLTFPBRMetallicRoughness : GLTFProperty
    {
        /// <summary>
        /// The RGBA components of the base color of the material.
        /// The fourth component (A) is the opacity of the material.
        /// These values are linear.
        /// </summary>
        public Color BaseColorFactor = Color.white;

        /// <summary>
        /// The base color texture.
        /// This texture contains RGB(A) components in sRGB color space.
        /// The first three components (RGB) specify the base color of the material.
        /// If the fourth component (A) is present, it represents the opacity of the
        /// material. Otherwise, an opacity of 1.0 is assumed.
        /// </summary>
        public GLTFTextureInfo BaseColorTexture;

        /// <summary>
        /// The metalness of the material.
        /// A value of 1.0 means the material is a metal.
        /// A value of 0.0 means the material is a dielectric.
        /// Values in between are for blending between metals and dielectrics such as
        /// dirty metallic surfaces.
        /// This value is linear.
        /// </summary>
        public double MetallicFactor = 1;

        /// <summary>
        /// The roughness of the material.
        /// A value of 1.0 means the material is completely rough.
        /// A value of 0.0 means the material is completely smooth.
        /// This value is linear.
        /// </summary>
        public double RoughnessFactor = 1;

        /// <summary>
        /// The metallic-roughness texture has two components.
        /// The first component (R) contains the metallic-ness of the material.
        /// The second component (G) contains the roughness of the material.
        /// These values are linear.
        /// If the third component (B) and/or the fourth component (A) are present,
        /// they are ignored.
        /// </summary>
        public GLTFTextureInfo MetallicRoughnessTexture;

        public static GLTFPBRMetallicRoughness Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var metallicRoughness = new GLTFPBRMetallicRoughness();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "baseColorFactor":
                        metallicRoughness.BaseColorFactor = reader.ReadAsRGBAColor();
                        break;
                    case "baseColorTexture":
                        metallicRoughness.BaseColorTexture = GLTFTextureInfo.Deserialize(root, reader);
                        break;
                    case "metallicFactor":
                        metallicRoughness.MetallicFactor = reader.ReadAsDouble().Value;
                        break;
                    case "roughnessFactor":
                        metallicRoughness.RoughnessFactor = reader.ReadAsDouble().Value;
                        break;
                    case "metallicRoughnessTexture":
                        metallicRoughness.MetallicRoughnessTexture = GLTFTextureInfo.Deserialize(root, reader);
                        break;
					default:
						metallicRoughness.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return metallicRoughness;
        }
    }
}
