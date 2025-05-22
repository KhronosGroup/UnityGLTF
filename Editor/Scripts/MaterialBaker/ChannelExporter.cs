using System;
using System.IO;
using UnityEditor;
using UnityEngine;

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
                SaveMaps(maps);
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
                SaveMaps(maps, 1);
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

        private static void SaveMaps(MaterialBaker.PbrMaps maps, int uvChannel = 0)
        {
            var material = maps.forMaterial;
            var mesh = maps.forMesh;
            
            var mergedAlbedoAndAlpha = new Texture2D(maps.albedo.map.width, maps.albedo.map.height, TextureFormat.RGBA32, false);
            var pixels = maps.albedo.map.GetPixels();
            var alphaPixels = maps.alpha.map.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
                pixels[i].a = alphaPixels[i].r;
            mergedAlbedoAndAlpha.SetPixels(pixels);

            var orm = new Texture2D(maps.metallic.map.width, maps.metallic.map.height, TextureFormat.RGBA32, false);
            var metallicPixels = maps.metallic.map.GetPixels();
            var occlusionPixels = maps.occlusion.map.GetPixels();
            var smoothnessPixels = maps.smoothness.map.GetPixels();
            for (var i = 0; i < metallicPixels.Length; i++)
            {
                var metallicValue = metallicPixels[i].r;
                var occlusionValue = occlusionPixels[i].r;
                var smoothnessValue = smoothnessPixels[i].r;
                orm.SetPixel(i % orm.width, i / orm.width, new Color(occlusionValue, 1 - smoothnessValue, metallicValue));
            }

            var baseColor = mergedAlbedoAndAlpha.EncodeToPNG();
            var normal = maps.normal.map.EncodeToPNG();
            var emission = maps.emission.map.EncodeToPNG();
            var ormTexture = orm.EncodeToPNG();

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
                var meshName = $"{mesh.name} ({meshGuid})";
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
            
            File.WriteAllBytes(baseColorPath, baseColor);
            File.WriteAllBytes(normalPath, normal);
            File.WriteAllBytes(emissionPath, emission);
            File.WriteAllBytes(ormPath, ormTexture);

            AssetDatabase.Refresh();

            // set the import settings for these textures. baseColor and emission are sRGB, normal is normal map, orm is linear
            var baseColorImporter = AssetImporter.GetAtPath(baseColorPath) as TextureImporter;
            baseColorImporter.textureType = TextureImporterType.Default;
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            normalImporter.textureType = TextureImporterType.NormalMap;
            var emissionImporter = AssetImporter.GetAtPath(emissionPath) as TextureImporter;
            emissionImporter.textureType = TextureImporterType.Default;
            emissionImporter.sRGBTexture = true;
            var ormImporter = AssetImporter.GetAtPath(ormPath) as TextureImporter;
            ormImporter.textureType = TextureImporterType.Default;
            ormImporter.sRGBTexture = false;

            baseColorImporter.SaveAndReimport();
            normalImporter.SaveAndReimport();
            ormImporter.SaveAndReimport();
            emissionImporter.SaveAndReimport();

            var importedBaseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            var importedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var importedOrm = AssetDatabase.LoadAssetAtPath<Texture2D>(ormPath);
            var importedEmission = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);

            var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!newMaterial) newMaterial = new Material(Shader.Find("UnityGLTF/PBRGraph"));
            
            newMaterial.SetTexture("baseColorTexture", importedBaseColor);
            newMaterial.SetTexture("normalTexture", importedNormal);
            newMaterial.SetTexture("metallicRoughnessTexture", importedOrm);
            newMaterial.SetTexture("emissiveTexture", importedEmission);
            newMaterial.SetTexture("occlusionTexture", importedOrm);

            var mapper = new PBRGraphMap(newMaterial);
            
            // Ensure multiplicative defaults
            mapper.MetallicFactor = 1;
            mapper.RoughnessFactor = 1;
            mapper.EmissiveFactor = Color.white;
            mapper.OcclusionTexStrength = 1;
            mapper.BaseColorFactor = Color.white;
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
            
            if (!maps.metallic.hasDefaultTransform)
            {
                mapper.MetallicRoughnessXOffset = maps.metallic.offset;
                mapper.MetallicRoughnessXScale = maps.metallic.scale;
            }
            if (!maps.normal.hasDefaultTransform)
            {
                mapper.NormalXOffset = maps.normal.offset;
                mapper.NormalXScale = maps.normal.scale;
            }
            if (!maps.emission.hasDefaultTransform)
            {
                mapper.EmissiveXOffset = maps.emission.offset;
                mapper.EmissiveXScale = maps.emission.scale;
            }
            if (!maps.occlusion.hasDefaultTransform)
            {
                mapper.OcclusionXOffset = maps.occlusion.offset;
                mapper.OcclusionXScale = maps.occlusion.scale;
            }
            if (!maps.albedo.hasDefaultTransform)
            {
                mapper.BaseColorXOffset = maps.albedo.offset;
                mapper.BaseColorXScale = maps.albedo.scale;
            }
            
            
            var applyTextureTransforms = false;
            if (applyTextureTransforms)
            {
                var baseColorTilingOffset = material.GetColor("Global_Tiling_Offset");
                GLTFMaterialHelper.SetKeyword(newMaterial, "_TEXTURE_TRANSFORM", true);
                var tiling = new Vector2(baseColorTilingOffset.r, baseColorTilingOffset.g);
                var offset = new Vector2(baseColorTilingOffset.b, baseColorTilingOffset.a);
                newMaterial.SetTextureScale("baseColorTexture", tiling);
                newMaterial.SetTextureOffset("baseColorTexture", offset);
                newMaterial.SetTextureScale("normalTexture", tiling);
                newMaterial.SetTextureOffset("normalTexture", offset);
                newMaterial.SetTextureScale("metallicRoughnessTexture", tiling);
                newMaterial.SetTextureOffset("metallicRoughnessTexture", offset);
                newMaterial.SetTextureScale("emissiveTexture", tiling);
                newMaterial.SetTextureOffset("emissiveTexture", offset);
                newMaterial.SetTextureScale("occlusionTexture", tiling);
                newMaterial.SetTextureOffset("occlusionTexture", offset);
            }
            
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