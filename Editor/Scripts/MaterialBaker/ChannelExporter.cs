using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
    internal static class ChannelExporter
    {
        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Bake in UV0 space")]
        private static void ExportChannelsUV0Space(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer) return;
            
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var maps = MaterialBaker.BakePBRMaterial(renderer, i, 2048, 2048);
                maps.forMaterial = materials[i];
                SaveMaps(maps, 0, false);
            }
        }
        
        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Bake in UV1 space")]
        private static void ExportChannelsUV1Space(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer) return;
            
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var maps = MaterialBaker.BakePBRMaterial(renderer, i, 2048, 2048, 1);
                SaveMaps(maps, 1, false);
            }
        }
        
        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Bake in texture space")]
        private static void ExportChannels(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer) return;

            var materials = renderer.sharedMaterials;
            foreach (var material in materials)
            {
                var maps = MaterialBaker.BakePBRMaterial(material, 2048, 2048);
                SaveMaps(maps);
            }
        }

        private static void SaveMaps(MaterialBaker.PbrMaps maps, int uvChannel = 0, bool useTextureSpace = true)
        {
            var material = maps.forMaterial;
            var mesh = maps.forMesh;

            var textureSize = maps.GetTextureSize();

            var path = AssetDatabase.GetAssetPath(material);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("No directory found for the material.");
                return;
            }

            var targetDirectory = "";
            var directoryForMaterial = Path.Combine(directory, fileName);
            if (!Directory.Exists(directoryForMaterial))
                Directory.CreateDirectory(directoryForMaterial);

            if (mesh)
            {
                var meshPath = AssetDatabase.GetAssetPath(mesh);
                var meshGuid = AssetDatabase.AssetPathToGUID(meshPath);
                var meshName = $"{mesh.name}";
                var directoryForMesh = Path.Combine(directoryForMaterial, meshName);
                if (!Directory.Exists(directoryForMesh))
                    Directory.CreateDirectory(directoryForMesh);
                
                targetDirectory = directoryForMesh;
            }
            else
            {
                targetDirectory = directoryForMaterial;
            }
            
            var baseColorPath = Path.Combine(targetDirectory, fileName + "_baseColor.png");
            var normalPath = Path.Combine(targetDirectory, fileName + "_normal.png");
            var emissionPath = Path.Combine(targetDirectory, fileName + "_emission.png");
            var ormPath = Path.Combine(targetDirectory, fileName + "_orm.png");
            var materialPath = Path.Combine(targetDirectory, fileName + ".mat");

            // if (maps.mask != null)
            // {
            //     var maskPath = Path.Combine(targetDirectory, fileName + "_mask.png");
            //     var maskTexture = maps.mask.map.EncodeToPNG();
            //     File.WriteAllBytes(maskPath, maskTexture);
            // }

            bool hasBaseColor = false;
            bool hasNormal = false;
            bool hasOrm = false;
            bool hasEmission = false;
            
            Color baseColor = Color.white;
            Color emissionColor = Color.black;
            float metallicFactor = 0f;
            float roughnessFactor = 1f;
            
            if (maps.albedo != null || maps.alpha != null)
            {
                var mergedAlbedoAndAlpha = new Texture2D(textureSize.width, textureSize.height, TextureFormat.RGBA32, false);

                Color[] pixels =  maps.albedo?.map.GetPixels();
                Color[] alphaPixels = maps.alpha?.map.GetPixels();
                
                if (pixels == null)
                {
                    pixels = new Color[alphaPixels.Length];
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color(0,0,0 , alphaPixels[i].r);
                }
                else if (alphaPixels == null)
                {
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i].a = 1f;
                }
                else
                {
                    for (var i = 0; i < pixels.Length; i++)
                        pixels[i].a = alphaPixels[i].r;
                }
                
                mergedAlbedoAndAlpha.SetPixels(pixels);

                if (MaterialBaker.TextureHasSingleValue(mergedAlbedoAndAlpha, out var singleBaseColorTex, maps.mask?.map))
                {
                    baseColor = singleBaseColorTex;
                    Object.DestroyImmediate(mergedAlbedoAndAlpha);
                }
                else
                {
                    var baseColorPng = mergedAlbedoAndAlpha.EncodeToPNG();
                    File.WriteAllBytes(baseColorPath, baseColorPng);
                    hasBaseColor = true;
                }
            }

            if (maps.metallic != null || maps.occlusion != null || maps.smoothness != null)
            {
                var orm = new Texture2D(textureSize.width, textureSize.height, TextureFormat.RGBA32, false, true);
          
                bool metallicHasSingleValue = MaterialBaker.TextureHasSingleValue(maps.metallic?.map, out var metallicColorTex, maps.mask?.map);
                bool smoothnessHasSingleValue = MaterialBaker.TextureHasSingleValue(maps.smoothness?.map, out var smoothnessColorTex, maps.mask?.map);
                bool occlusionHasSingleValue = MaterialBaker.TextureHasSingleValue(maps.occlusion?.map, out var occlusionColorTex, maps.mask?.map);

                bool metallicSingleValueOrEmpty = maps.metallic == null || metallicHasSingleValue;
                bool smoothnessSingleValueOrEmpty = maps.smoothness == null || smoothnessHasSingleValue;
                bool occlusionSingleValueOrEmpty = maps.occlusion == null || (occlusionHasSingleValue && occlusionColorTex == Color.white);
                
                if (occlusionSingleValueOrEmpty && metallicSingleValueOrEmpty && smoothnessSingleValueOrEmpty)
                {
                    metallicFactor = metallicHasSingleValue ? metallicColorTex.r : 0;
                    roughnessFactor = smoothnessHasSingleValue ? (1f -  smoothnessColorTex.r) : 0f;
                }
                else
                {
                    var metallicPixels = maps.metallic?.map.GetPixels();
                    var occlusionPixels = maps.occlusion?.map.GetPixels();
                    var smoothnessPixels = maps.smoothness?.map.GetPixels();
                    var length = metallicPixels?.Length ?? occlusionPixels?.Length ?? smoothnessPixels?.Length ?? 0;
                    
                    var ormPixels = new Color[length];
                    for (var i = 0; i < length; i++)
                    {
                        var metallicValue = metallicPixels?[i].r ?? 0f;
                        var occlusionValue = occlusionPixels?[i].r ?? 0f;
                        var smoothnessValue = smoothnessPixels?[i].r ?? 0f;
                        ormPixels[i] = new Color(occlusionValue, 1 - smoothnessValue, metallicValue);
                    }

                    orm.SetPixels(ormPixels);

                    var ormTexture = orm.EncodeToPNG();
                    File.WriteAllBytes(ormPath, ormTexture);
                    hasOrm = true;
                }
            }

            if (maps.normal != null)
            {
                if (!MaterialBaker.TextureHasSingleValue(maps.normal.map, out _, maps.mask?.map))
                {
                    var normal = maps.normal.map.EncodeToPNG();
                    File.WriteAllBytes(normalPath, normal);
                    hasNormal = true;
                }
            }

            if (maps.emission != null)
            {
                if (MaterialBaker.TextureHasSingleValue(maps.emission.map, out var emissionColorTex, maps.mask?.map))
                {
                    emissionColor = emissionColorTex;
                }
                else
                {
                    var emission = maps.emission.map.EncodeToPNG();
                    emissionColor = Color.white;
                    File.WriteAllBytes(emissionPath, emission);
                    hasEmission = true; 
                }
            }

            AssetDatabase.Refresh();

            // set the import settings for these textures. baseColor and emission are sRGB, normal is normal map, orm is linear
            var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!newMaterial) newMaterial = new Material(Shader.Find("UnityGLTF/PBRGraph"));
            
            if (hasBaseColor)
            {
                var baseColorImporter = AssetImporter.GetAtPath(baseColorPath) as TextureImporter;
                baseColorImporter.textureType = TextureImporterType.Default;
                baseColorImporter.SaveAndReimport();
                var importedBaseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
                newMaterial.SetTexture("baseColorTexture", importedBaseColor);
            }
            else
                newMaterial.SetTexture("baseColorTexture", null);

            if (hasNormal)
            {
                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
                var importedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                newMaterial.SetTexture("normalTexture", importedNormal);
            }
            else
                newMaterial.SetTexture("normalTexture", null);

            if (hasEmission)
            {
                var emissionImporter = AssetImporter.GetAtPath(emissionPath) as TextureImporter;
                emissionImporter.textureType = TextureImporterType.Default;
                emissionImporter.sRGBTexture = true;
                emissionImporter.SaveAndReimport();
                var importedEmission = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
                newMaterial.SetTexture("emissiveTexture", importedEmission);
            }
            else
                newMaterial.SetTexture("emissiveTexture", null);

            if (hasOrm)
            {
                var ormImporter = AssetImporter.GetAtPath(ormPath) as TextureImporter;
                ormImporter.textureType = TextureImporterType.Default;
                ormImporter.sRGBTexture = false;
                ormImporter.SaveAndReimport();
                var importedOrm = AssetDatabase.LoadAssetAtPath<Texture2D>(ormPath);
                newMaterial.SetTexture("metallicRoughnessTexture", importedOrm);
                newMaterial.SetTexture("occlusionTexture", importedOrm);
            }
            else
            {
                newMaterial.SetTexture("metallicRoughnessTexture", null);
                newMaterial.SetTexture("occlusionTexture", null);

            }

            var mapper = new PBRGraphMap(newMaterial);
            
            // Ensure multiplicative defaults
            mapper.MetallicFactor = metallicFactor;
            mapper.RoughnessFactor = roughnessFactor;
            mapper.EmissiveFactor = emissionColor;
            mapper.OcclusionTexStrength =  hasOrm ? 1f : 0f;
            mapper.BaseColorFactor = baseColor;
            mapper.NormalTexScale = 1;
            // Set desired UV channels – based on the space we baked into
            mapper.BaseColorTexCoord = uvChannel;
            mapper.NormalTexCoord = uvChannel;
            mapper.EmissiveTexCoord = uvChannel;
            mapper.OcclusionTexCoord = uvChannel;
            mapper.MetallicRoughnessTexCoord = uvChannel;
            
            // TODO set Opaque/Transparent based on the original material
            // TODO set alpha cutoff based on the original material
            // TODO set double sided based on the original material
            // TODO set texture tiling and offset based on the original material
            // HACK for a specific material that stores tiling/offset in a color property
            
            bool anyTextureTransform = false;
            if (hasOrm)
            {
                if (maps.metallic != null && !maps.metallic.hasDefaultTransform)
                {
                    mapper.MetallicRoughnessXOffset = maps.metallic.offset;
                    mapper.MetallicRoughnessXScale = maps.metallic.scale;
                }
                else
                if (maps.smoothness != null && !maps.smoothness.hasDefaultTransform)
                {
                    mapper.MetallicRoughnessXOffset = maps.smoothness.offset;
                    mapper.MetallicRoughnessXScale = maps.smoothness.scale;
                }
                else
                if (maps.occlusion != null && !maps.occlusion.hasDefaultTransform)
                {
                    mapper.MetallicRoughnessXOffset = maps.occlusion.offset;
                    mapper.MetallicRoughnessXScale = maps.occlusion.scale;
                }
                else
                {
                    mapper.MetallicRoughnessXOffset = Vector2.zero;
                    mapper.MetallicRoughnessXScale = Vector2.one;
                }
                anyTextureTransform = true;
            }
            if (hasNormal && !maps.normal.hasDefaultTransform)
            {
                mapper.NormalXOffset = maps.normal.offset;
                mapper.NormalXScale = maps.normal.scale;
                anyTextureTransform = true;
            }
            else
            {
                mapper.NormalXOffset = Vector2.zero;
                mapper.NormalXScale = Vector2.one;
            }
            
            if (hasEmission && !maps.emission.hasDefaultTransform)
            {
                mapper.EmissiveXOffset = maps.emission.offset;
                mapper.EmissiveXScale = maps.emission.scale;
                anyTextureTransform = true;
            }
            else
            {
                mapper.EmissiveXOffset = Vector2.zero;
                mapper.EmissiveXScale = Vector2.one;
            }
            
            if (hasOrm && !maps.occlusion.hasDefaultTransform)
            {
                mapper.OcclusionXOffset = maps.occlusion.offset;
                mapper.OcclusionXScale = maps.occlusion.scale;
                anyTextureTransform = true;
            }
            else
            {
                mapper.OcclusionXOffset = Vector2.zero;
                mapper.OcclusionXScale = Vector2.one;
            }
            
            if (hasBaseColor && maps.albedo != null && !maps.albedo.hasDefaultTransform)
            {
                mapper.BaseColorXOffset = maps.albedo.offset;
                mapper.BaseColorXScale = maps.albedo.scale;
                anyTextureTransform = true;
            }
            else
            if (hasBaseColor && maps.alpha != null && !maps.alpha.hasDefaultTransform)
            {
                mapper.BaseColorXOffset = maps.alpha.offset;
                mapper.BaseColorXScale = maps.alpha.scale;
                anyTextureTransform = true;
            }
            else
            {
                mapper.BaseColorXOffset = Vector2.zero;
                mapper.BaseColorXScale = Vector2.one;
            }

            GLTFMaterialHelper.SetKeyword(newMaterial, "_TEXTURE_TRANSFORM", anyTextureTransform);
            
            // if (useTextureSpace)
            // {
            //     if (material.HasColor("Global_Tiling_Offset"))
            //     {
            //         var baseColorTilingOffset = material.GetColor("Global_Tiling_Offset");
            //         GLTFMaterialHelper.SetKeyword(newMaterial, "_TEXTURE_TRANSFORM", true);
            //         var tiling = new Vector2(baseColorTilingOffset.r, baseColorTilingOffset.g);
            //         var offset = new Vector2(baseColorTilingOffset.b, baseColorTilingOffset.a);
            //         newMaterial.SetTextureScale("baseColorTexture", tiling);
            //         newMaterial.SetTextureOffset("baseColorTexture", offset);
            //         newMaterial.SetTextureScale("normalTexture", tiling);
            //         newMaterial.SetTextureOffset("normalTexture", offset);
            //         newMaterial.SetTextureScale("metallicRoughnessTexture", tiling);
            //         newMaterial.SetTextureOffset("metallicRoughnessTexture", offset);
            //         newMaterial.SetTextureScale("emissiveTexture", tiling);
            //         newMaterial.SetTextureOffset("emissiveTexture", offset);
            //         newMaterial.SetTextureScale("occlusionTexture", tiling);
            //         newMaterial.SetTextureOffset("occlusionTexture", offset);
            //     }
            // }
            
            if (!AssetDatabase.Contains(newMaterial))
                AssetDatabase.CreateAsset(newMaterial, materialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Switch to converted material")]
        private static void SwitchToConvertedMaterial(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer) return;

            var materials = renderer.sharedMaterials;
            var newMaterials = new Material[materials.Length];
            Array.Copy(materials, newMaterials, materials.Length);

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];

                var path = AssetDatabase.GetAssetPath(material);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    Debug.LogWarning("No directory found for the material.");
                    continue;
                }

                var newDirectory = Path.Combine(directory, fileName);
                if (!Directory.Exists(newDirectory))
                {
                    Debug.LogWarning("No directory found for the material.");
                    continue;
                }

                var newMaterialPath = Path.Combine(newDirectory, fileName + ".mat");
                var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
                if (!newMaterial)
                {
                    Debug.LogWarning("No material found at " + newMaterialPath);
                    continue;
                }

                newMaterials[i] = newMaterial;
            }

            Undo.RegisterCompleteObjectUndo(renderer, "Switch to converted material");
            renderer.sharedMaterials = newMaterials;
        }

        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Switch to original material")]
        private static void SwitchToOriginalMaterial(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer) return;

            var materials = renderer.sharedMaterials;
            var newMaterials = new Material[materials.Length];
            Array.Copy(materials, newMaterials, materials.Length);

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];

                var path = AssetDatabase.GetAssetPath(material);
                // one directory up, same file name
                var fileName = Path.GetFileNameWithoutExtension(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    Debug.LogWarning("No directory found for the material.");
                    continue;
                }

                var newDirectory = Path.GetDirectoryName(directory);
                ;
                if (!Directory.Exists(newDirectory))
                {
                    Debug.LogWarning("No directory found for the material.");
                    continue;
                }

                var newMaterialPath = Path.Combine(newDirectory, fileName + ".mat");
                var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
                if (!newMaterial)
                {
                    Debug.LogWarning("No material found at " + newMaterialPath);
                    continue;
                }
                
                newMaterials[i] = newMaterial;
            }

            Undo.RegisterCompleteObjectUndo(renderer, "Switch to original material");
            renderer.sharedMaterials = newMaterials;
        }
    }
}