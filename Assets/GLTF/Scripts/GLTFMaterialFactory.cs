using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLTF;
using UnityEngine;

namespace GLTF
{
    public interface IGLTFMaterialFactory
    {
        Material CreateMaterial(GLTFMaterial def);
    }

    public class GLTFStandardMaterialFactory : IGLTFMaterialFactory
    {
        public Material CreateMaterial(GLTFMaterial def)
        {
            Material material = new Material(Shader.Find("GLTF/GLTFMetallicRoughness"));

            if (def.pbrMetallicRoughness != null)
            {
                GLTFPBRMetallicRoughness pbr = def.pbrMetallicRoughness;

                material.SetColor("_BaseColorFactor", pbr.baseColorFactor);

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

            if (def.normalTexture != null)
            {
                GLTFTexture texture = def.normalTexture.index.Value;
                material.SetTexture("_NormalMap", texture.Texture);
                material.SetTextureScale("_NormalMap", new Vector2(1, -1));
                material.SetFloat("_NormalScale", (float)def.normalTexture.scale);
            }

            if (def.occlusionTexture != null)
            {
                GLTFTexture texture = def.occlusionTexture.index.Value;
                material.SetTexture("_OcclusionMap", texture.Texture);
                material.SetTextureScale("_OcclusionMap", new Vector2(1, -1));
                material.SetFloat("_OcclusionStrength", (float)def.occlusionTexture.strength);
            }

            if (def.emissiveTexture != null)
            {
                GLTFTexture texture = def.emissiveTexture.index.Value;
                material.SetTextureScale("_EmissiveMap", new Vector2(1, -1));
                material.SetTexture("_EmissiveMap", texture.Texture);
            }

            material.SetColor("_EmissiveFactor", def.emissiveFactor);

            return material;
        }
    }
}
