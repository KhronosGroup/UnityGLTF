using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace UnityGLTF
{
    public class TextureImporterHelper
    {
        private const string DefaultTextureAssetGuid = "7b7ff3cec11c24c599d6f12443877d5e";
        
        public static TextureImporterFormat GetAutomaticFormat(Texture2D texture)
        {
            var defaultTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(DefaultTextureAssetGuid)) as TextureImporter;
            // defaultTextureImporter = new TextureImporter();
            
            TextureImporterSettings importerSettings = new TextureImporterSettings();
            
            importerSettings.aniso = 5;
            importerSettings.wrapMode = texture.wrapMode;
            importerSettings.filterMode = texture.filterMode;
            importerSettings.mipmapEnabled = texture.mipmapCount > 1;
            importerSettings.alphaSource = TextureImporterAlphaSource.FromInput;
            
            var hasAlpha = TextureUtil.HasAlphaTextureFormat(texture.format);
            var isHDR = TextureUtil.IsHDRFormat(texture.format);
           
            // var platformSettings = new TextureImporterPlatformSettings();
            foreach (BuildPlatform validPlatform in BuildPlatforms.instance.GetValidPlatforms())
            {
                // TextureImporter.RecommendedFormatsFromTextureTypeAndPlatform
                if (validPlatform.IsActive())
                {
                    return TextureImporter.DefaultFormatFromTextureParameters(importerSettings, defaultTextureImporter.GetPlatformTextureSettings(validPlatform.name), hasAlpha, isHDR, validPlatform.defaultTarget);
                }
            }
            
            // This should never happen
            return TextureImporterFormat.Automatic;
        }
    }
}