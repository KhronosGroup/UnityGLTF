using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGLTF
{
    internal static class ChannelExporter
    {
        [MenuItem("CONTEXT/MeshRenderer/UnityGLTF/Export Channels")]
        private static void ExportChannels(MenuCommand command)
        {
            var renderer = command.context as Renderer;
            if (!renderer)
            {
                Debug.LogError("No Renderer found.");
                return;
            }

            var materials = renderer.sharedMaterials;
            foreach (var material in materials)
            {
                var maps = MaterialBaker.BakePbrMataterial(material);

                var mergedAlbedoAndAlpha =
                    new Texture2D(maps.albedo.width, maps.albedo.height, TextureFormat.RGBA32, false);
                var pixels = maps.albedo.GetPixels();
                var alphaPixels = maps.alpha.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i].a = alphaPixels[i].r;
                mergedAlbedoAndAlpha.SetPixels(pixels);

                var orm = new Texture2D(maps.metallic.width, maps.metallic.height, TextureFormat.RGBA32, false);
                var metallicPixels = maps.metallic.GetPixels();
                var occlusionPixels = maps.occlusion.GetPixels();
                var smoothnessPixels = maps.smoothness.GetPixels();
                for (var i = 0; i < metallicPixels.Length; i++)
                {
                    var metallicValue = metallicPixels[i].r;
                    var occlusionValue = occlusionPixels[i].r;
                    var smoothnessValue = smoothnessPixels[i].r;
                    orm.SetPixel(i % orm.width, i / orm.width,
                        new Color(occlusionValue, 1 - smoothnessValue, metallicValue));
                }

                var baseColor = mergedAlbedoAndAlpha.EncodeToPNG();
                var normal = maps.normal.EncodeToPNG();
                var emission = maps.emission.EncodeToPNG();
                var ormTexture = orm.EncodeToPNG();

                var path = AssetDatabase.GetAssetPath(material);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    Debug.LogError("No directory found for the material.");
                    return;
                }

                var newDirectory = Path.Combine(directory, fileName);
                if (!Directory.Exists(newDirectory))
                {
                    Directory.CreateDirectory(newDirectory);
                }

                var baseColorPath = Path.Combine(newDirectory, fileName + "_baseColor.png");
                var normalPath = Path.Combine(newDirectory, fileName + "_normal.png");
                var emissionPath = Path.Combine(newDirectory, fileName + "_emission.png");
                var ormPath = Path.Combine(newDirectory, fileName + "_orm.png");
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

                // create new UnityGLTF/PBRGraph material with these textures
                var newMaterial = new Material(Shader.Find("UnityGLTF/PBRGraph"));
                newMaterial.SetTexture("baseColorTexture", importedBaseColor);
                newMaterial.SetTexture("normalTexture", importedNormal);
                newMaterial.SetTexture("metallicRoughnessTexture", importedOrm);
                newMaterial.SetTexture("emissiveTexture", importedEmission);
                newMaterial.SetTexture("occlusionTexture", importedOrm);

                var mapper = new PBRGraphMap(newMaterial);
                mapper.MetallicFactor = 1;
                mapper.RoughnessFactor = 1;
                // TODO set Opaque/Transparent based on the original material
                // TODO set alpha cutoff based on the original material
                // TODO set double sided based on the original material

                AssetDatabase.CreateAsset(newMaterial, Path.Combine(newDirectory, fileName + ".mat"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
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

                Debug.Log("Loading material from " + newMaterialPath, newMaterial);

                newMaterials[i] = newMaterial;
            }

            Undo.RegisterCompleteObjectUndo(renderer, "Switch to original material");
            renderer.sharedMaterials = newMaterials;
        }
    }
}