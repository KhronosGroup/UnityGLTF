using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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

            public Material forMaterial;
            public Mesh forMesh;
        }

        public static PbrMaps BakePBRMaterial(Material material, int width, int height)
        {
            var pbrMaps = new PbrMaps();
            pbrMaps.forMaterial = material;
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

        public static PbrMaps BakePBRMaterial(Renderer renderer, int submesh, int width, int height, int uvChannel = 0)
        {
            var pbrMaps = new PbrMaps();
            var materials = renderer.sharedMaterials;
            pbrMaps.forMaterial = materials[submesh % materials.Length];
            pbrMaps.forMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            
            foreach (var shader in PatchedShaders)
            {
                var pair = shader.Value;
                if (!pair) continue;
                Object.DestroyImmediate(pair);
            }
            PatchedShaders.Clear();
            MeshUVs.Clear();
            
#if HAVE_URP
            pbrMaps.albedo = Bake(renderer, submesh, DebugMaterialMode.Albedo, width, height, uvChannel);
            pbrMaps.alpha = Bake(renderer, submesh, DebugMaterialMode.Alpha, width, height, uvChannel);
            pbrMaps.metallic = Bake(renderer, submesh, DebugMaterialMode.Metallic, width, height, uvChannel);
            pbrMaps.normal = Bake(renderer, submesh, DebugMaterialMode.NormalTangentSpace, width, height, uvChannel);
            pbrMaps.occlusion = Bake(renderer, submesh, DebugMaterialMode.AmbientOcclusion, width, height, uvChannel);
            pbrMaps.emission = Bake(renderer, submesh, DebugMaterialMode.Emission, width, height, uvChannel);
            pbrMaps.smoothness = Bake(renderer, submesh, DebugMaterialMode.Smoothness, width, height, uvChannel);
            pbrMaps.specular = Bake(renderer, submesh, DebugMaterialMode.Specular, width, height, uvChannel);
#endif
            return pbrMaps;
        }
    
        private static readonly Dictionary<(Shader shader, int uvChannel), Shader> PatchedShaders = new Dictionary<(Shader shader, int uvChannel), Shader>();
        private static readonly Dictionary<(Mesh mesh, int uvChannel), (Vector2 minMaxX, Vector2 minMaxY)> MeshUVs = new Dictionary<(Mesh mesh, int uvChannel), (Vector2 minMaxX, Vector2 minMaxY)>();
        
#if HAVE_URP
        public static Texture2D Bake(Renderer renderer, int submesh, DebugMaterialMode mode, int width, int height, int uvChannel)
        {
            DeactivateGlobalUrpDebugProperties();
            
            // TODO: submeshes
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var materials = renderer.sharedMaterials;
            var sourceMaterial = materials[submesh % materials.Length];
            
            var isLinear = IsDebugMaterialModeInLinear(mode);
            
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            
            var material = Object.Instantiate(sourceMaterial);
            material.hideFlags = HideFlags.DontSave;
            
            var pair = (material.shader, uvChannel);
            if (!PatchedShaders.TryGetValue(pair, out var patched))
            {
                var patchedShader = ShaderModifier.PatchShaderUVsToClipSpace(material.shader, uvChannel);
                PatchedShaders[pair] = patchedShader;
                material.shader = patchedShader;
            }
            else
            {
                material.shader = patched;
            }
          
            var cmd = new CommandBuffer();
            GL.sRGBWrite = !isLinear;
            
            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.blue);
            
            // TODO we probably need to find the UV extents of the source mesh and set the viewport accordingly; otherwise we end up with a wrong space here.
            // We also might need to adjust the texture transform of the material to match the UV extents after baking,
            // so we don't have to modify UV coordinates of the mesh.
            var meshRangePair = (mesh, uvChannel);
            if (!MeshUVs.TryGetValue(meshRangePair, out var minMax))
            {
                var meshUVs = mesh.uv;
                var xRange = new Vector2(float.MaxValue, float.MinValue);
                var yRange = new Vector2(float.MaxValue, float.MinValue);
                foreach (var uv in meshUVs)
                {
                    xRange.x = Mathf.Min(xRange.x, uv.x);
                    xRange.y = Mathf.Max(xRange.y, uv.x);
                    yRange.x = Mathf.Min(yRange.x, uv.y);
                    yRange.y = Mathf.Max(yRange.y, uv.y);
                }
                minMax = (xRange, yRange);
                MeshUVs[meshRangePair] = minMax;
            }
            
            var minMaxX = minMax.minMaxX;
            var minMaxY = minMax.minMaxY;

            // Regular case – UVs are in 0..1 range. We might not want to introduce texture transforms for this case.
            if (minMaxX.x >= 0 && minMaxX.y <= 1 && minMaxY.x > 0 && minMaxY.y <= 1)
            {
                minMaxX.x = 0;
                minMaxX.y = 1;
                minMaxY.x = 0;
                minMaxY.y = 1;
            }
            
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(minMaxX.x, minMaxX.y, minMaxY.x, minMaxY.y, -1, 1));
            cmd.EnableKeyword(new GlobalKeyword(ShaderKeywordStrings.DEBUG_DISPLAY));
            cmd.SetGlobalFloat("_DebugMaterialMode", (int) mode);
            cmd.DrawMesh(mesh, Matrix4x4.identity, material, submesh, 0);
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
            var bakedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, isLinear);
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

        private static bool IsDebugMaterialModeInLinear(DebugMaterialMode mode)
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
            return isLinear;
        }
        
        private static void BakeUrpMaterialModeToTexture(Material mat, DebugMaterialMode mode, int textureWidth, int textureHeight, out Texture2D bakedTexture)
        {
            bool isLinear = IsDebugMaterialModeInLinear(mode);
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
            GL.sRGBWrite = !isLinear;
            
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