#if HAVE_URP_12_OR_NEWER || HAVE_URP_10_OR_NEWER

using UnityEngine;
using UnityEngine.Rendering;
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
		    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		    {
			    if (renderingData.cameraData.isPreviewCamera)
				    return;
			    base.Execute(context, ref renderingData);
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
	        public new void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
	        {
	            base.Setup(source, destination, downsampling);

	            this.destination = destination;
	            this.m_DownsamplingMethod = downsampling;
	        }
#endif
	        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
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
		        RenderingUtils.ReAllocateIfNeeded(ref m_destination, descriptor, FilterMode.Trilinear, TextureWrapMode.Clamp, name: CAMERA_OPAQUE_TEXTURENAME);
		        base.Setup(m_source, m_destination, this.m_DownsamplingMethod);
		        cmd.SetGlobalTexture(m_destination.name, m_destination.nameID);
#else
	            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Trilinear);
#endif
	        }
	    }

	    CustomRenderPass m_ScriptablePass;

#if !UNITY_2022_3_OR_NEWER
	    RenderTargetHandle m_OpaqueColor;
#endif

	    /// <inheritdoc/>
	    public override void Create()
	    {
#if UNITY_2022_3_OR_NEWER
		    if (m_ScriptablePass == null)
		    {
			    m_ScriptablePass = new CustomRenderPass(RenderPassEvent.AfterRenderingSkybox);
		    }
#else
	        m_OpaqueColor.Init(CAMERA_OPAQUE_TEXTURENAME);
#endif
	    }

	    // Here you can inject one or multiple render passes in the renderer.
	    // This method is called when setting up the renderer once per-camera.
	    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	    {

#if UNITY_2022_3_OR_NEWER
			renderer.EnqueuePass(m_ScriptablePass);
#else
	        if (m_ScriptablePass == null)
	        {
	            m_ScriptablePass = new CustomRenderPass(RenderPassEvent.AfterRenderingSkybox);
	        }

#if UNITY_2022_3_OR_NEWER
		    var identifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
		    m_ScriptablePass.Setup(identifier, m_OpaqueColor, downsampling);
#else
	        m_ScriptablePass.Setup(renderer.cameraColorTarget, m_OpaqueColor, downsampling);
#endif
	        renderer.EnqueuePass(m_ScriptablePass);
#endif
	    }

#if UNITY_2022_3_OR_NEWER
		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
		{
			m_ScriptablePass.Setup(renderer.cameraColorTargetHandle, downsampling);
		}

		public void OnDestroy()
		{
			m_ScriptablePass?.Dispose();
		}
#endif

	}
}

#endif
