using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
 
#if HAVE_URP_12_OR_NEWER
    [DisallowMultipleRendererFeature("Opaque Texture (Rough Refractions) - RenderGraph")]
#endif
public class RoughRefractionFeature : ScriptableRendererFeature
{
   
    class RoughRefractionPass : ScriptableRenderPass
    {
        // This class stores the data that the render pass needs. The RecordRenderGraph method populates the data and the render graph passes it as a parameter to the rendering function.
        class PassData
        {
            internal TextureHandle activeColorTexture;
            internal TextureHandle destinationTexture;
        }
        
        // Rendering function that generates the rendering commands for the render pass.
        // The RecordRenderGraph method instructs the render graph to use it with the SetRenderFunc method.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.activeColorTexture, new Vector4(1, 1, 0, 0), 0, false);
        }
 
        // This method adds and configures one or more render passes in the render graph.
        // This process includes declaring their inputs and outputs, but does not include adding commands to command buffers.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Rough Refraction Pass";
 
            // Add a raster render pass to the render graph. The PassData type parameter determines the type of the passData out variable
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // UniversalResourceData contains all the texture handles used by URP, including the active color and depth textures of the camera
 
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
 
                // Populate passData with the data needed by the rendering function of the render pass
 
                // Use the camera’s active color texture as the source texture for the copy
                passData.activeColorTexture = resourceData.activeColorTexture;
 
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureDesc rgDesc = new TextureDesc(cameraData.cameraTargetDescriptor.width / 2, cameraData.cameraTargetDescriptor.height / 2);
                rgDesc.name = "_CameraOpaqueTexture";
                rgDesc.dimension = cameraData.cameraTargetDescriptor.dimension;
                rgDesc.clearBuffer = false;
                rgDesc.autoGenerateMips = true;
                rgDesc.useMipMap = true;
                rgDesc.msaaSamples = MSAASamples.MSAA4x;
                rgDesc.filterMode = FilterMode.Bilinear;
                rgDesc.wrapMode = TextureWrapMode.Clamp;

                rgDesc.mipMapBias = 1f;
                rgDesc.bindTextureMS = cameraData.cameraTargetDescriptor.bindMS;
                rgDesc.colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat;
                rgDesc.depthBufferBits = 0;
                rgDesc.isShadowMap = false;
                rgDesc.vrUsage = cameraData.cameraTargetDescriptor.vrUsage;
                
                //TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_CameraOpaqueTexture", false);

                passData.destinationTexture = renderGraph.CreateTexture(rgDesc);;

                builder.UseTexture(passData.activeColorTexture);

                builder.SetRenderAttachment(passData.destinationTexture, 0, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(passData.destinationTexture, Shader.PropertyToID("_CameraOpaqueTexture"));
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
 
    RoughRefractionPass m_CopyRenderPass;
    
    public override void Create()
    {
        m_CopyRenderPass = new RoughRefractionPass();
        // Configure the injection point in which URP runs the pass
        m_CopyRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
 
    // URP calls this method every frame, once for each Camera. This method lets you inject ScriptableRenderPass instances into the scriptable Renderer.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_CopyRenderPass);
    }
}