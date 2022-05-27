using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class CopyColorBuiltin : MonoBehaviour
{
	private RenderTexture opaqueTexture;
	private RenderTexture temp;
	private void OnDisable()
	{
		if(temp != null)
			RenderTexture.ReleaseTemporary(temp);
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		var dsc = src.descriptor;
		dsc.useMipMap = true;
		dsc.autoGenerateMips = true;

		RenderTexture.ReleaseTemporary(temp);
		temp = RenderTexture.GetTemporary(dsc);

		Graphics.Blit(src, temp);
		Graphics.Blit(src, dest);

		Shader.SetGlobalTexture("_CameraOpaqueTexture", temp);
	}
}
