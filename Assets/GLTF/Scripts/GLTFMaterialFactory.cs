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
            var material = new Material(Shader.Find("GLTF/GLTFMetallicRoughness"));

            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                material.SetColor("_BaseColorFactor", pbr.BaseColorFactor);

                if (pbr.BaseColorTexture != null)
                {
                    var texture = pbr.BaseColorTexture.Index.Value;
                    material.SetTexture("_MainTex", texture.Texture);
                    material.SetTextureScale("_MainTex", new Vector2(1, -1));
                }

                material.SetFloat("_MetallicFactor", (float)pbr.MetallicFactor);

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var texture = pbr.MetallicRoughnessTexture.Index.Value;
                    material.SetTexture("_MetallicRoughnessMap", texture.Texture);
                    material.SetTextureScale("_MetallicRoughnessMap", new Vector2(1, -1));
                }

                material.SetFloat("_RoughnessFactor", (float)pbr.RoughnessFactor);
            }

            if (def.NormalTexture != null)
            {
                var texture = def.NormalTexture.Index.Value;
                material.SetTexture("_NormalMap", texture.Texture);
                material.SetTextureScale("_NormalMap", new Vector2(1, -1));
                material.SetFloat("_NormalScale", (float)def.NormalTexture.Scale);
            }

            if (def.OcclusionTexture != null)
            {
                var texture = def.OcclusionTexture.Index.Value;
                material.SetTexture("_OcclusionMap", texture.Texture);
                material.SetTextureScale("_OcclusionMap", new Vector2(1, -1));
                material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
            }

            if (def.EmissiveTexture != null)
            {
                var texture = def.EmissiveTexture.Index.Value;
                material.SetTextureScale("_EmissiveMap", new Vector2(1, -1));
                material.SetTexture("_EmissiveMap", texture.Texture);
            }

            material.SetColor("_EmissiveFactor", def.EmissiveFactor);

            return material;
        }
    }
}
