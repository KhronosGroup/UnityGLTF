using UnityEngine;
using UnityGLTF;

public class SampleModelList : MonoBehaviour
{
	public static string LoaderFieldName => nameof(loader);
	public static string PathRootFieldName => nameof(pathRoot);
	public static string ManifestRelativePathFieldName => nameof(manifestRelativePath);
	public static string ModelRelativePathFieldName => nameof(modelRelativePath);
	public static string LoadThisFrameFieldName => nameof(loadThisFrame);

	[SerializeField]
	private GLTFComponent loader = null;

	[SerializeField]
	private string pathRoot = "http://localhost:8080/glTF-Sample-Models/2.0/";

// Disable "unused private field" because it is accessed by SampleModelListInspector using serialization.
#pragma warning disable 414
	[SerializeField]
	private string manifestRelativePath = "model-index.json";
#pragma warning restore 414

	[SerializeField]
	private string modelRelativePath = null;

	[SerializeField]
	private bool loadThisFrame = false;

	private async void Update()
	{
		if (loadThisFrame)
		{
			loadThisFrame = false;

			var path = pathRoot + modelRelativePath;

			Debug.Log(path, this);

			if (loader.LastLoadedScene != null)
			{
				Destroy(loader.LastLoadedScene);
			}
			loader.GLTFUri = path;
			await loader.Load();
		}
	}
}
