using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace UnityGLTF
{
    public class TextureImporterHelper
    {
        private static MethodInfo GetFixedPlatformName;
        private const string DefaultTextureAssetGuid = "7b7ff3cec11c24c599d6f12443877d5e";
        
        public static TextureImporterFormat GetAutomaticFormat(Texture2D texture, string platform)
        {
            var defaultTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(DefaultTextureAssetGuid)) as TextureImporter;
            // defaultTextureImporter = new TextureImporter();

            if (GetFixedPlatformName == null)
                GetFixedPlatformName = typeof(TextureImporter)
                    .GetMethod("GetFixedPlatformName", BindingFlags.Static | BindingFlags.NonPublic);
            
            if (GetFixedPlatformName == null)
                throw new MissingMethodException("TextureImporter.GetFixedPlatformName");

            platform = GetFixedPlatformName.Invoke(null, new object[] { platform }) as string;
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
                if (validPlatform.name == platform)
                    return TextureImporter.DefaultFormatFromTextureParameters(importerSettings, defaultTextureImporter.GetPlatformTextureSettings(platform), hasAlpha, isHDR, validPlatform.defaultTarget);
            }
            
            // This should never happen
            return TextureImporterFormat.Automatic;
        }
    }
}