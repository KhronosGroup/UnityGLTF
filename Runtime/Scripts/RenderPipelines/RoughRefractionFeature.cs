#if HAVE_URP_12_OR_NEWER || HAVE_URP_10_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityGLTF
{
#if HAVE_URP_12_OR_NEWER
	[DisallowMultipleRendererFeature("Opaque Texture (Rough Refractions)")]
#endif
	public class RoughRefractionFeature : ScriptableRendererFeature
	{
		private const string CAMERA_OPAQUE_TEXTURENAME = "_CameraOpaqueTexture";

#if !UNITY_2022_3_OR_NEWER
	    [SerializeField]
#endif
		private Downsampling downsampling = Downsampling.None;

	    class CustomRenderPass : CopyColorPass
	    {
	        public Downsampling m_DownsamplingMethod;
#if UNITY_2022_3_OR_NEWER
			public RTHandle m_destination;
			public RTHandle m_source;

#else
	        public RenderTargetHandle destination;
#endif
		    public CustomRenderPass(RenderPassEvent evt) : base(evt,
			    CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Sampling")),
	            CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Blit")))
			{ }

#if UNITY_2022_3_OR_NEWER
#pragma warning disable 672
		    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
#pragma warning restore 672
		    {
			    if (renderingData.cameraData.isPreviewCamera)
				    return;
			    
			    if (m_source == null || m_destination == null || !m_source.rt || !m_destination.rt)
				    return;
				     
#pragma warning disable 672
#pragma warning disable 618
			    base.Execute(context, ref renderingData);
#pragma warning restore 672
#pragma warning restore 618
		    }

		    public void Setup(RTHandle source, Downsampling downsampling)
		    {
			    this.m_source = source;
			    this.m_DownsamplingMethod = downsampling;
		    }

		    public void Dispose()
		    {
			    m_destination?.Release();
		    }
#else
		    
		    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		    {
			    if (renderingData.cameraData.isPreviewCamera)
				    return;
			    
			    Setup(renderingData.cameraData.renderer.cameraColorTarget, destination, m_DownsamplingMethod);
				     
#pragma warning disable 672
#pragma warning disable 618
			    base.Execute(context, ref renderingData);
#pragma warning restore 672
#pragma warning restore 618
		    }		    
	        public new void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
	        {
	            base.Setup(source, destination, downsampling);

	            this.destination = destination;
	            this.m_DownsamplingMethod = downsampling;
	        }
#endif

#pragma warning disable 672
		    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
#pragma warning restore 672
	        {
	            var desc = renderingData.cameraData.cameraTargetDescriptor;
	            desc.useMipMap = true;
	            desc.autoGenerateMips = true;
	            renderingData.cameraData.cameraTargetDescriptor = desc;

	            // base.OnCameraSetup(cmd, ref renderingData);

	            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
	            descriptor.msaaSamples = 1;
	            descriptor.depthBufferBits = 0;
	            if (m_DownsamplingMethod == Downsampling._2xBilinear)
	            {
	                descriptor.width /= 2;
	                descriptor.height /= 2;
	            }
	            else if (m_DownsamplingMethod == Downsampling._4xBox || m_DownsamplingMethod == Downsampling._4xBilinear)
	            {
	                descriptor.width /= 4;
	                descriptor.height /= 4;
	            }
#if UNITY_2022_3_OR_NEWER

#if UNITY_6000_0_OR_NEWER
		        RenderingUtils.ReAllocateHandleIfNeeded(ref m_destination, descriptor, FilterMode.Trilinear, TextureWrapMode.Clamp, name: CAMERA_OPAQUE_TEXTURENAME);
#else
		        RenderingUtils.ReAllocateIfNeeded(ref m_destination, descriptor, FilterMode.Trilinear, TextureWrapMode.Clamp, name: CAMERA_OPAQUE_TEXTURENAME);
#endif
		        base.Setup(m_source, m_destination, this.m_DownsamplingMethod);
		        cmd.SetGlobalTexture(m_destination.name, m_destination.nameID);
#else
	            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Trilinear);
#endif
	        }
	    }

	    private CustomRenderPass m_RoughRefractionPassNonRG;
#if UNITY_2023_3_OR_NEWER
	    private bool usingRenderGraph = false;
#endif

#if !UNITY_2022_3_OR_NEWER
	    RenderTargetHandle m_OpaqueColor;
#endif
		
		/// <inheritdoc/>
	    public override void Create()
	    {
#if UNITY_2023_3_OR_NEWER
		    var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
		    usingRenderGraph = !renderGraphSettings.enableRenderCompatibilityMode;
		    if (!usingRenderGraph)
		    {
#endif
#if UNITY_2022_3_OR_NEWER
			    if (m_RoughRefractionPassNonRG == null)
			    {
				    m_RoughRefractionPassNonRG = new CustomRenderPass(RenderPassEvent.AfterRenderingSkybox);
			    }
#else
				m_OpaqueColor.Init(CAMERA_OPAQUE_TEXTURENAME);
#endif

#if UNITY_2023_3_OR_NEWER
		    }
		    else
		    {
			    if (m_RoughRefractionPassRG == null)
			    {
				    m_RoughRefractionPassRG = new RoughRefractionPassRG();
				    m_RoughRefractionPassRG.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
			    }
		    }
#endif		    
	    }

	    // Here you can inject one or multiple render passes in the renderer.
	    // This method is called when setting up the renderer once per-camera.
	    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	    {
		    if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
			    return;
		    
#if UNITY_2022_3_OR_NEWER
		    if (m_RoughRefractionPassNonRG != null)
		    {
				renderer.EnqueuePass(m_RoughRefractionPassNonRG);
		    }
#if UNITY_2023_3_OR_NEWER
		    else
		    if (usingRenderGraph && m_RoughRefractionPassRG != null)
		    {
			    renderer.EnqueuePass(m_RoughRefractionPassRG);
		    }
#endif
#else
	        if (m_RoughRefractionPassNonRG == null)
	        {
	            m_RoughRefractionPassNonRG = new CustomRenderPass(RenderPassEvent.AfterRenderingSkybox);
	        }

#if UNITY_2022_3_OR_NEWER
		    var identifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
		    m_RoughRefractionPassNonRG.Setup(identifier, m_OpaqueColor, downsampling);
#else
		    
	        m_RoughRefractionPassNonRG.Setup(renderer.cameraColorTarget, m_OpaqueColor, downsampling);
#endif
	        renderer.EnqueuePass(m_RoughRefractionPassNonRG);
#endif
	    }
	    
#if UNITY_2022_3_OR_NEWER
		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
		{
#pragma warning disable 618
			m_RoughRefractionPassNonRG.Setup(renderer.cameraColorTargetHandle, downsampling);
#pragma warning restore 618
		}

		public void OnDestroy()
		{
			m_RoughRefractionPassNonRG?.Dispose();
		}
#endif
		
#if UNITY_2023_3_OR_NEWER
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
	                rgDesc.filterMode = FilterMode.Bilinear;
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
#endif

	}
}

#endif
