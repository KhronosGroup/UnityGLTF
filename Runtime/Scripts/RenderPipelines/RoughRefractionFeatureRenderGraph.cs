#if UNITY_6000_4_OR_NEWER

// RenderGraph Version for Unity 6.4+

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UnityGLTF
{
	[DisallowMultipleRendererFeature("Opaque Texture (Rough Refractions)")]
	public class RoughRefractionFeature : ScriptableRendererFeature
	{

		/// <inheritdoc/>
	    public override void Create()
	    {
		    if (m_RoughRefractionPassRG == null)
		    {
			    m_RoughRefractionPassRG = new RoughRefractionPassRG();
			    m_RoughRefractionPassRG.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
		    }
	    }

	    // Here you can inject one or multiple render passes in the renderer.
	    // This method is called when setting up the renderer once per-camera.
	    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	    {
		    if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
			    return;
		    
		    if (m_RoughRefractionPassRG != null)
		    {
			    renderer.EnqueuePass(m_RoughRefractionPassRG);
		    }
	    }
		
		private RoughRefractionPassRG m_RoughRefractionPassRG;

		// ######### RenderGraph Version #########
		class RoughRefractionPassRG : ScriptableRenderPass
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
		        var rtHandle = (RTHandle) data.activeColorTexture;
		        // The implicit conversion seems to mess up when calling RenderToCubemap() programmatically,
		        // so we need to do the conversion ourselves and check validity
		        if (rtHandle.rt || rtHandle.externalTexture)
					Blitter.BlitTexture(context.cmd, rtHandle, new Vector4(1, 1, 0, 0), 0, false);
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

	                TextureDesc rgDesc = new TextureDesc(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
	                rgDesc.name = "_CameraOpaqueTexture";
	                rgDesc.dimension = cameraData.cameraTargetDescriptor.dimension;
	                rgDesc.clearBuffer = false;
	                rgDesc.autoGenerateMips = true;
	                rgDesc.useMipMap = true;
	                rgDesc.msaaSamples = MSAASamples.None;
	                rgDesc.filterMode = FilterMode.Trilinear;
	                rgDesc.wrapMode = TextureWrapMode.Clamp;

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
	                
	                builder.SetRenderFunc((RoughRefractionFeature.RoughRefractionPassRG.PassData data, RasterGraphContext context) => ExecutePass(data, context));
	            }
	        }
		}

	}
}

#endif
