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
	    public Downsampling downsampling = Downsampling.None;

	    class CustomRenderPass : CopyColorPass
	    {
	        public Downsampling m_DownsamplingMethod;
	        public RenderTargetHandle destination;

	        public CustomRenderPass(RenderPassEvent evt) : base(evt,
	            CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Sampling")),
	            CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Blit")))
	        { }

	        public new void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
	        {
	            base.Setup(source, destination, downsampling);

	            this.destination = destination;
	            this.m_DownsamplingMethod = downsampling;
	        }

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

	            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Trilinear);
	        }
	    }

	    CustomRenderPass m_ScriptablePass;
	    RenderTargetHandle m_OpaqueColor;

	    /// <inheritdoc/>
	    public override void Create()
	    {
	        m_OpaqueColor.Init("_CameraOpaqueTexture");
	    }

	    // Here you can inject one or multiple render passes in the renderer.
	    // This method is called when setting up the renderer once per-camera.
	    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	    {
	        if (m_ScriptablePass == null)
	        {
	            m_ScriptablePass = new CustomRenderPass(RenderPassEvent.AfterRenderingSkybox);
	        }
	        m_ScriptablePass.Setup(renderer.cameraColorTarget, m_OpaqueColor, downsampling);

	        renderer.EnqueuePass(m_ScriptablePass);
	    }
	}
}

#endif
