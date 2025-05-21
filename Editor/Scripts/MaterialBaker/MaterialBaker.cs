using System;
using UnityEngine;
using Object = UnityEngine.Object;
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

        public static PbrMaps BakePBRMaterial(Material material, int width, int height)
        {
            var pbrMaps = new PbrMaps();
#if HAVE_URP
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Albedo, width, height, out pbrMaps.albedo);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Alpha, width, height, out pbrMaps.alpha);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Metallic, width, height, out pbrMaps.metallic);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.NormalTangentSpace, width, height, out pbrMaps.normal);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.AmbientOcclusion, width, height, out pbrMaps.occlusion);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Emission, width, height, out pbrMaps.emission);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Smoothness, width, height, out pbrMaps.smoothness);
            BakeUrpMaterialModeToTexture(material, DebugMaterialMode.Specular, width, height, out pbrMaps.specular);
#endif
            return pbrMaps;
        }
    
#if HAVE_URP
        
        public static Texture2D Bake(Renderer renderer, DebugMaterialMode mode)
        {
            // TODO: submeshes
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            
            var rt = RenderTexture.GetTemporary(1024, 1024, 0, RenderTextureFormat.ARGB32);
            var material = Object.Instantiate(renderer.sharedMaterial);
        
            var patchedShader = ShaderModifier.PatchShaderUVsToClipSpace(material.shader);
            material.shader = patchedShader;
          
            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.blue);
            
            // Set cmd.SetViewProjectionMatrices to get the correct view projection matrix to the mesh
            
            cmd.EnableKeyword(new GlobalKeyword(ShaderKeywordStrings.DEBUG_DISPLAY));
            cmd.SetGlobalFloat("_DebugMaterialMode", (int)mode);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
            Graphics.ExecuteCommandBuffer(cmd);
            //
            // cmd.Clear();
            // cmd.DisableKeyword(new GlobalKeyword(ShaderKeywordStrings.DEBUG_DISPLAY));
            // Graphics.ExecuteCommandBuffer(cmd);
            //
      
            
           //  Graphics.SetRenderTarget(rt);
           //  var rp = new RenderParams(material);
           //  rp.receiveShadows = false;
           //  rp.shadowCastingMode = ShadowCastingMode.Off;
           //  
           //  Shader.EnableKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);
           //  Shader.SetGlobalFloat("_DebugMaterialMode", (int)mode);
           //  
           // // Graphics.RenderMesh(rp, renderer.GetComponent<MeshFilter>().sharedMesh, 0, Matrix4x4.identity);
           //  material.SetPass(0);
           //  Graphics.DrawMeshNow( renderer.GetComponent<MeshFilter>().sharedMesh, Matrix4x4.identity, 0);
           //
            RenderTexture.active = rt;
            var bakedTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            bakedTexture.wrapMode = TextureWrapMode.Repeat;
            bakedTexture.filterMode = FilterMode.Bilinear;
            bakedTexture.anisoLevel = 1;
            bakedTexture.Apply();
            // Read pixels from renderTexture and apply to bakedTexture
            bakedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
      
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            
            Shader.DisableKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);

            return bakedTexture;
            

        }

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
        
        private static void BakeUrpMaterialModeToTexture(Material mat, DebugMaterialMode mode, int textureWidth, int textureHeight, out Texture2D bakedTexture)
        {
            bool isLinear = false;
            switch (mode)
            {
                case DebugMaterialMode.Alpha:
                case DebugMaterialMode.Smoothness:
                case DebugMaterialMode.AmbientOcclusion:
                case DebugMaterialMode.NormalWorldSpace:
                case DebugMaterialMode.NormalTangentSpace:
                case DebugMaterialMode.LightingComplexity:
                case DebugMaterialMode.Metallic:
                case DebugMaterialMode.SpriteMask:
                    isLinear = true;
                    break;
            }
            var bakeMat = new Material(mat);
            
            var resetTextureTransforms = false;
            if (resetTextureTransforms)
            {
                // reset texture transform properties
                var props = new string[] {
                    "Base_Tiling_Offset",
                    "Global_Tiling_Offset",
                    "_Detail_Tiling_Offset",
                    "_Normal_Detail_Tiling_Offset",
                    "_Normal_Tiling_Offset",
                    "_Smoothness_Detail_Tiling_Offset",
                    "_Smoothness_Tiling_Offset",
                    "_Metallic_Tiling_Offset",
                };
                foreach (var prop in props)
                {
                    if (mat.HasProperty(prop))
                    {
                        bakeMat.SetColor(prop, new Color(1, 1, 0, 0));
                    }
                }
            }
            
            Shader.EnableKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);
            Shader.SetGlobalFloat("_DebugMaterialMode", (int)mode);
      
            DeactivateGlobalUrpDebugProperties();
            
            bakedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, isLinear);
            bakedTexture.wrapMode = TextureWrapMode.Repeat;
            bakedTexture.filterMode = FilterMode.Bilinear;
            bakedTexture.anisoLevel = 1;
            bakedTexture.Apply();
            
            // Render mesh with bakeMat to bakedTexture
            RenderTexture renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32, isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
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