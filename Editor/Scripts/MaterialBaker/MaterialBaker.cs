using UnityEngine;
#if HAVE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UnityGLTF
{
    public static class MaterialBaker
    {
          public class PbrMaps
        {
            public Texture2D albedo;
            public Texture2D alpha;
            public Texture2D metallic;
            public Texture2D normal;
            public Texture2D occlusion;
            public Texture2D emission;
            public Texture2D smoothness;
            public Texture2D specular;
        }

        public static PbrMaps BakePbrMataterial(Material material)
        {
            var pbrMaps = new PbrMaps();
#if HAVE_URP
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Albedo, out pbrMaps.albedo);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Alpha, out pbrMaps.alpha);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Metallic, out pbrMaps.metallic);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.NormalTangentSpace, out pbrMaps.normal);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.AmbientOcclusion, out pbrMaps.occlusion);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Emission, out pbrMaps.emission);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Smoothness, out pbrMaps.smoothness);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Specular, out pbrMaps.specular);
#endif
            return pbrMaps;
        }
    
#if HAVE_URP

        private static void DeactivateGlobalUrpDebugProperties()
        {
            // See DebugHandler.cs in URP package
            
           Shader.SetGlobalFloat("_DebugVertexAttributeMode", 0);

            Shader.SetGlobalInteger("_DebugMaterialValidationMode", 0);

            // Rendering settings...
            Shader.SetGlobalInteger("k_DebugMipInfoModeId", 0);
            Shader.SetGlobalInteger("_DebugSceneOverrideMode", 0);
            Shader.SetGlobalInteger("_DebugFullScreenMode", 0);
            Shader.SetGlobalInteger("_DebugValidationMode", 0);

            // Lighting settings...
            Shader.SetGlobalFloat("_DebugLightingMode", 0);
            Shader.SetGlobalInteger("_DebugLightingFeatureFlags", 0);
        }
        
        public static void BakeUrpMaterialModeToTexture(Material mat, DebugMaterialMode mode, out Texture2D bakedTexture)
        {
            var bakeMat = new Material(mat);
            Shader.EnableKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);
            Shader.SetGlobalFloat("_DebugMaterialMode", (int)mode);
      
            DeactivateGlobalUrpDebugProperties();
            
            bakedTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            bakedTexture.wrapMode = TextureWrapMode.Repeat;
            bakedTexture.filterMode = FilterMode.Bilinear;
            bakedTexture.anisoLevel = 1;
            bakedTexture.Apply();
            
            // Render mesh with bakeMat to bakedTexture
            RenderTexture renderTexture = RenderTexture.GetTemporary(1024, 1024, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(bakedTexture, renderTexture, bakeMat, 0);
           
            RenderTexture.active = renderTexture;

            // Read pixels from renderTexture and apply to bakedTexture
            bakedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
      
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(bakeMat);
            
            Shader.DisableKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);
        }
#endif
    }
}