using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    public class GLTFMaterial
    {
        public string name;

        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness
        /// material model from Physically-Based Rendering (PBR) methodology.
        /// </summary>
        public GLTFPBRMetallicRoughness pbrMetallicRoughness;

        /// <summary>
        /// A tangent space normal map. Each texel represents the XYZ components of a
        /// normal vector in tangent space.
        /// </summary>
        public GLTFNormalTextureInfo normalTexture;

        /// <summary>
        /// The occlusion map is a greyscale texture, with white indicating areas that
        /// should receive full indirect lighting and black indicating no indirect
        /// lighting.
        /// </summary>
        public GLTFOcclusionTextureInfo occlusionTexture;

        /// <summary>
        /// The emissive map controls the color and intensity of the light being emitted
        /// by the material. This texture contains RGB components in sRGB color space.
        /// If a fourth component (A) is present, it is ignored.
        /// </summary>
        public GLTFTextureInfo emissiveTexture;

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
        public double[] emissiveFactor = { 0.0f, 0.0f, 0.0f };

        private Material material;

        /// <summary>
        /// Construct or return the Unity Material for this GLTFMaterial.
        /// </summary>
        public Material Material
        {
            get
            {
                if (material != null)
                {
                    return material;
                }

                material = new Material(Shader.Find("GLTF/GLTFMetallicRoughness"));

                if (pbrMetallicRoughness != null)
                {
                    GLTFPBRMetallicRoughness pbr = pbrMetallicRoughness;

                    double[] colorFactor = pbr.baseColorFactor;
                    Color baseColor = new Color((float)colorFactor[0], (float)colorFactor[1], (float)colorFactor[2], (float)colorFactor[3]);
                    material.SetColor("_BaseColorFactor", baseColor);

                    if (pbr.baseColorTexture != null)
                    {
                        GLTFTexture texture = pbr.baseColorTexture.index.Value;
                        material.SetTexture("_MainTex", texture.Texture);
                        material.SetTextureScale("_MainTex", new Vector2(1, -1));
                    }

                    material.SetFloat("_MetallicFactor", (float)pbr.metallicFactor);

                    if (pbr.metallicRoughnessTexture != null)
                    {
                        GLTFTexture texture = pbr.metallicRoughnessTexture.index.Value;
                        material.SetTexture("_MetallicRoughnessMap", texture.Texture);
                        material.SetTextureScale("_MetallicRoughnessMap", new Vector2(1, -1));
                    }

                    material.SetFloat("_RoughnessFactor", (float)pbr.roughnessFactor);
                }

                if (normalTexture != null)
                {
                    GLTFTexture texture = normalTexture.index.Value;
                    material.SetTexture("_NormalMap", texture.Texture);
                    material.SetTextureScale("_NormalMap", new Vector2(1, -1));
                    material.SetFloat("_NormalScale", (float)normalTexture.scale);
                }

                if (occlusionTexture != null)
                {
                    GLTFTexture texture = occlusionTexture.index.Value;
                    material.SetTexture("_OcclusionMap", texture.Texture);
                    material.SetTextureScale("_OcclusionMap", new Vector2(1, -1));
                    material.SetFloat("_OcclusionStrength", (float)occlusionTexture.strength);
                }

                if (emissiveTexture != null)
                {
                    GLTFTexture texture = emissiveTexture.index.Value;
                    material.SetTextureScale("_EmissiveMap", new Vector2(1, -1));
                    material.SetTexture("_EmissiveMap", texture.Texture);
                }

                Color emissiveColor = new Color((float)emissiveFactor[0], (float)emissiveFactor[1], (float)emissiveFactor[2], 1.0f);
                material.SetColor("_EmissiveFactor", emissiveColor);

                return material;
            }
        }
    }

    /// <summary>
    /// Reference to a texture.
    /// </summary>
    public class GLTFTextureInfoBase
    {
        /// <summary>
        /// The index of the texture.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFTextureId index;

        /// <summary>
        /// This integer value is used to construct a string in the format
        /// TEXCOORD_<set index> which is a reference to a key in
        /// mesh.primitives.attributes (e.g. A value of 0 corresponds to TEXCOORD_0).
        /// </summary>
        public int texCoord = 0;
    }

    public class GLTFTextureInfo : GLTFTextureInfoBase { }

    public class GLTFNormalTextureInfo : GLTFTextureInfoBase
    {
        /// <summary>
        /// The scalar multiplier applied to each normal vector of the texture.
        /// This value is ignored if normalTexture is not specified.
        /// This value is linear.
        /// </summary>
        public double scale = 1.0f;
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
        public double strength = 1.0f;
    }

    /// <summary>
    /// A set of parameter values that are used to define the metallic-roughness
    /// material model from Physically-Based Rendering (PBR) methodology.
    /// </summary>
    public class GLTFPBRMetallicRoughness
    {
        /// <summary>
        /// The RGBA components of the base color of the material.
        /// The fourth component (A) is the opacity of the material.
        /// These values are linear.
        /// </summary>
        public double[] baseColorFactor = { 1, 1, 1, 1 };

        /// <summary>
        /// The base color texture.
        /// This texture contains RGB(A) components in sRGB color space.
        /// The first three components (RGB) specify the base color of the material.
        /// If the fourth component (A) is present, it represents the opacity of the
        /// material. Otherwise, an opacity of 1.0 is assumed.
        /// </summary>
        public GLTFTextureInfo baseColorTexture;

        /// <summary>
        /// The metalness of the material.
        /// A value of 1.0 means the material is a metal.
        /// A value of 0.0 means the material is a dielectric.
        /// Values in between are for blending between metals and dielectrics such as
        /// dirty metallic surfaces.
        /// This value is linear.
        /// </summary>
        public double metallicFactor = 1;

        /// <summary>
        /// The roughness of the material.
        /// A value of 1.0 means the material is completely rough.
        /// A value of 0.0 means the material is completely smooth.
        /// This value is linear.
        /// </summary>
        public double roughnessFactor = 1;

        /// <summary>
        /// The metallic-roughness texture has two components.
        /// The first component (R) contains the metallic-ness of the material.
        /// The second component (G) contains the roughness of the material.
        /// These values are linear.
        /// If the third component (B) and/or the fourth component (A) are present,
        /// they are ignored.
        /// </summary>
        public GLTFTextureInfo metallicRoughnessTexture;
    }
}
