using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RoughRefraction : MonoBehaviour
{
	private readonly Dictionary<Camera, RenderTexture> renderTextureCache = new Dictionary<Camera, RenderTexture>();
	private static readonly int CameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");

	private void OnDisable()
	{
		if (renderTextureCache != null) {
			foreach(var kvp in renderTextureCache)
				RenderTexture.ReleaseTemporary(kvp.Value);
		}
		renderTextureCache?.Clear();
	}

	private void OnPreRender()
	{
		SetTexture();
	}

	private void SetTexture()
	{
		var current = Camera.current;
		if (!renderTextureCache.ContainsKey(current))
		{
			Shader.SetGlobalTexture(CameraOpaqueTexture, Texture2D.blackTexture);
			return;
		}

		Shader.SetGlobalTexture(CameraOpaqueTexture, renderTextureCache[current]);
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		var dsc = src.descriptor;
		dsc.useMipMap = true;
		dsc.autoGenerateMips = false;
		dsc.msaaSamples = 1;
		dsc.width = Mathf.ClosestPowerOfTwo(dsc.width);
		dsc.height = Mathf.ClosestPowerOfTwo(dsc.height);

		var current = Camera.current;
		if (!renderTextureCache.ContainsKey(current))
			renderTextureCache.Add(current, null);
		if(renderTextureCache[current])
			RenderTexture.ReleaseTemporary(renderTextureCache[current]);

		renderTextureCache[current] = RenderTexture.GetTemporary(dsc);
		renderTextureCache[current].filterMode = FilterMode.Trilinear;
		// temp[current].useMipMap = true;

		Graphics.Blit(src, renderTextureCache[current]);
		renderTextureCache[current].GenerateMips();
		Graphics.Blit(src, dest);

		SetTexture();
	}
}
