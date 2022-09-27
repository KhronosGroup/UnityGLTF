using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RoughRefraction : MonoBehaviour
{
	public Renderer tempOutput;
	private MaterialPropertyBlock tempBlock;

	private RenderTexture opaqueTexture;
	private Dictionary<Camera, RenderTexture> temp = new Dictionary<Camera, RenderTexture>();

	private void OnDisable()
	{
		if (temp != null) {
			foreach(var kvp in temp)
				RenderTexture.ReleaseTemporary(kvp.Value);
		}
		temp.Clear();
	}

	private void OnPreRender()
	{
		SetTexture();
	}

	private void SetTexture()
	{
		var current = Camera.current;
		if (!temp.ContainsKey(current))
		{
			Shader.SetGlobalTexture("_CameraOpaqueTexture", Texture2D.blackTexture);
			return;
		}

		Shader.SetGlobalTexture("_CameraOpaqueTexture", temp[current]);

		if (tempOutput)
		{
			if (tempBlock == null) tempBlock = new MaterialPropertyBlock();
			tempBlock.SetTexture("_MainTex", temp[current]);
			tempOutput.SetPropertyBlock(tempBlock);
		}
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
		if (!temp.ContainsKey(current))
			temp.Add(current, null);
		if(temp[current])
			RenderTexture.ReleaseTemporary(temp[current]);

		temp[current] = RenderTexture.GetTemporary(dsc);
		temp[current].filterMode = FilterMode.Trilinear;
		// temp[current].useMipMap = true;

		Graphics.Blit(src, temp[current]);
		temp[current].GenerateMips();
		Graphics.Blit(src, dest);

		SetTexture();
	}
}
