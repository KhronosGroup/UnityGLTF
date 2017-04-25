using System;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// The material appearance of a primitive.
    /// </summary>
    public class GLTFMaterial : GLTFChildOfRootProperty
    {
        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness
        /// material model from Physically-Based Rendering (PBR) methodology.
        /// </summary>
        public GLTFPBRMetallicRoughness PbrMetallicRoughness;

        /// <summary>
        /// A tangent space normal map. Each texel represents the XYZ components of a
        /// normal vector in tangent space.
        /// </summary>
        public GLTFNormalTextureInfo NormalTexture;

        /// <summary>
        /// The occlusion map is a greyscale texture, with white indicating areas that
        /// should receive full indirect lighting and black indicating no indirect
        /// lighting.
        /// </summary>
        public GLTFOcclusionTextureInfo OcclusionTexture;

	    /// <summary>
	    /// The emissive map controls the color and intensity of the light being emitted
	    /// by the material. This texture contains RGB components in sRGB color space.
	    /// If a fourth component (A) is present, it is ignored.
	    /// </summary>
	    public GLTFTextureInfo EmissiveTexture;

		/// <summary>
		/// The RGB components of the emissive color of the material.
		/// If an emissiveTexture is specified, this value is multiplied with the texel
		/// values.
		/// <items>
		///     <minimum>0.0</minimum>
		///     <maximum>1.0</maximum>
		/// </items>
		/// <minItems>3</minItems>
		/// <maxItems>3</maxItems>
		/// </summary>
		public Color EmissiveFactor = Color.black;

        /// <summary>
        /// The material's alpha rendering mode enumeration specifying the interpretation of the
        /// alpha value of the main factor and texture. In `OPAQUE` mode, the alpha value is
        /// ignored and the rendered output is fully opaque. In `MASK` mode, the rendered output
        /// is either fully opaque or fully transparent depending on the alpha value and the
        /// specified alpha cutoff value. In `BLEND` mode, the alpha value is used to composite
        /// the source and destination areas. The rendered output is combined with the background
        /// using the normal painting operation (i.e. the Porter and Duff over operator).
        /// </summary>
        public GLTFAlphaMode AlphaMode = GLTFAlphaMode.OPAQUE;

        /// <summary>
        /// Specifies the cutoff threshold when in `MASK` mode. If the alpha value is greater than
        /// or equal to this value then it is rendered as fully opaque, otherwise, it is rendered
        /// as fully transparent. This value is ignored for other modes.
        /// </summary>
        public double AlphaCutoff = 0.5;

        /// <summary>
        /// Specifies whether the material is double sided. When this value is false, back-face
        /// culling is enabled. When this value is true, back-face culling is disabled and double
        /// sided lighting is enabled. The back-face must have its normals reversed before the
        /// lighting equation is evaluated.
        /// </summary>
        public bool DoubleSided;

        private Material _material;

        /// <summary>
        /// Construct or return the Unity Material for this GLTFMaterial.
        /// </summary>
        public Material GetMaterial(GLTFConfig config)
        {
            if (_material == null)
            {
				_material = config.MaterialFactory.CreateMaterial(this);
            }

            return _material;
        }

        public static GLTFMaterial Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var material = new GLTFMaterial();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "pbrMetallicRoughness":
                        material.PbrMetallicRoughness = GLTFPBRMetallicRoughness.Deserialize(root, reader);
                        break;
                    case "normalTexture":
                        material.NormalTexture = GLTFNormalTextureInfo.Deserialize(root, reader);
                        break;
                    case "occlusionTexture":
                        material.OcclusionTexture = GLTFOcclusionTextureInfo.Deserialize(root, reader);
                        break;
                    case "emissiveTexture":
                        material.EmissiveTexture = GLTFTextureInfo.Deserialize(root, reader);
                        break;
                    case "emissiveFactor":
                        material.EmissiveFactor = reader.ReadAsRGBColor();
                        break;
                    case "alphaMode":
                        material.AlphaMode = reader.ReadStringEnum<GLTFAlphaMode>();
                        break;
                    case "alphaCutoff":
                        material.AlphaCutoff = reader.ReadAsDouble().Value;
                        break;
                    case "doubleSided":
                        material.DoubleSided = reader.ReadAsBoolean().Value;
                        break;
					default:
						material.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return material;
        }
    }

    public enum GLTFAlphaMode
    {
        OPAQUE,
        MASK,
        BLEND
    }

    /// <summary>
    /// Reference to a texture.
    /// </summary>
    public class GLTFTextureInfoBase : GLTFProperty
    {
        /// <summary>
        /// The index of the texture.
        /// </summary>
        public GLTFTextureId Index;

        /// <summary>
        /// This integer value is used to construct a string in the format
        /// TEXCOORD_<set index> which is a reference to a key in
        /// mesh.primitives.attributes (e.g. A value of 0 corresponds to TEXCOORD_0).
        /// </summary>
        public int TexCoord = 0;
    }

    public class GLTFTextureInfo : GLTFTextureInfoBase
    {

        public static GLTFTextureInfo Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var textureInfo = new GLTFTextureInfo();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "index":
                        textureInfo.Index = GLTFTextureId.Deserialize(root, reader);
                        break;
                    case "texCoord":
                        textureInfo.TexCoord = reader.ReadAsInt32().Value;
                        break;
					default:
						textureInfo.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return textureInfo;
        }
    }

    public class GLTFNormalTextureInfo : GLTFTextureInfoBase
    {
        /// <summary>
        /// The scalar multiplier applied to each normal vector of the texture.
        /// This value is ignored if normalTexture is not specified.
        /// This value is linear.
        /// </summary>
        public double Scale = 1.0f;

        public static GLTFNormalTextureInfo Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var textureInfo = new GLTFNormalTextureInfo();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "index":  
                        textureInfo.Index = GLTFTextureId.Deserialize(root, reader);
                        break;
                    case "texCoord":
                        textureInfo.TexCoord = reader.ReadAsInt32().Value;
                        break;
                    case "scale":
                        textureInfo.Scale = reader.ReadAsDouble().Value;
                        break;
					default:
						textureInfo.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return textureInfo;
        }
    }

    public class GLTFOcclusionTextureInfo : GLTFTextureInfoBase
    {
        /// <summary>
        /// A scalar multiplier controlling the amount of occlusion applied.
        /// A value of 0.0 means no occlusion.
        /// A value of 1.0 means full occlusion.
        /// This value is ignored if the corresponding texture is not specified.
        /// This value is linear.
        /// <minimum>0.0</minimum>
        /// <maximum>1.0</maximum>
        /// </summary>
        public double Strength = 1.0f;

        public static GLTFOcclusionTextureInfo Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var textureInfo = new GLTFOcclusionTextureInfo();

            if (reader.Read() && reader.TokenType != JsonToken.StartObject)
            {
                throw new Exception("Asset must be an object.");
            }

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "index":
                        textureInfo.Index = GLTFTextureId.Deserialize(root, reader);
                        break;
                    case "texCoord":
                        textureInfo.TexCoord = reader.ReadAsInt32().Value;
                        break;
                    case "strength":
                        textureInfo.Strength = reader.ReadAsDouble().Value;
                        break;
					default:
						textureInfo.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return textureInfo;
        }
    }

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
