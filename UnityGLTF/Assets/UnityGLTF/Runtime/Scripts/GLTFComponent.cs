using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityGLTF.Loader;

namespace UnityGLTF
{
    /// <summary>
    /// Component to load a GLTF scene with
    /// </summary>
    public class GLTFComponent : MonoBehaviour
	{
		public string GLTFUri = null;
		public bool Multithreaded = true;
		public bool UseStream = false;
		public bool AppendStreamingAssets = true;
		public bool PlayAnimationOnLoad = true;
        public ImporterFactory Factory = null;
        public UnityAction onLoadComplete;

#if UNITY_ANIMATION
        public IEnumerable<Animation> Animations { get; private set; }
#endif

		[SerializeField]
		private bool loadOnStart = true;

		[SerializeField] private int RetryCount = 10;
		[SerializeField] private float RetryTimeout = 2.0f;
		private int numRetries = 0;


		public int MaximumLod = 300;
		public int Timeout = 8;
		public GLTFSceneImporter.ColliderType Collider = GLTFSceneImporter.ColliderType.None;
		public GameObject LastLoadedScene { get; private set; } = null;

		[SerializeField]
		private Shader shaderOverride = null;

		private async void Start()
		{
			if (!loadOnStart) return;

			try
			{
				await Load();
			}
#if WINDOWS_UWP
			catch (Exception)
#else
			catch (HttpRequestException)
#endif
			{
				if (numRetries++ >= RetryCount)
					throw;

				Debug.LogWarning("Load failed, retrying");
				await Task.Delay((int)(RetryTimeout * 1000));
				Start();
			}
		}

		public async Task Load()
		{
			var importOptions = new ImportOptions
			{
				AsyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ?? gameObject.AddComponent<AsyncCoroutineHelper>()
			};

			GLTFSceneImporter sceneImporter = null;
			try
			{
				var isFileUri = GLTFUri.StartsWith("file://");
                Factory = Factory ?? ScriptableObject.CreateInstance<DefaultImporterFactory>();

				if (UseStream || isFileUri)
				{
					string fullPath;
					if (AppendStreamingAssets && !isFileUri)
					{
						// Path.Combine treats paths that start with the separator character
						// as absolute paths, ignoring the first path passed in. This removes
						// that character to properly handle a filename written with it.
						fullPath = Path.Combine(Application.streamingAssetsPath, GLTFUri.TrimStart(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
					}
					else
					{
						fullPath = GLTFUri;
					}

					var uri = GLTFUri;
					if (isFileUri) uri = uri.Substring("file://".Length);
					string directoryPath = URIHelper.GetDirectoryName(uri);
					importOptions.DataLoader = new FileLoader(directoryPath);
					sceneImporter = Factory.CreateSceneImporter(
						Path.GetFileName(uri),
						importOptions
						);
				}
				else
				{
					string directoryPath = URIHelper.GetDirectoryName(GLTFUri);
					importOptions.DataLoader = new WebRequestLoader(directoryPath);

					sceneImporter = Factory.CreateSceneImporter(
						URIHelper.GetFileFromUri(new Uri(GLTFUri)),
						importOptions
						);

				}

				sceneImporter.SceneParent = gameObject.transform;
				sceneImporter.Collider = Collider;
				sceneImporter.MaximumLod = MaximumLod;
				sceneImporter.Timeout = Timeout;
				sceneImporter.IsMultithreaded = Multithreaded;
				sceneImporter.CustomShaderName = shaderOverride ? shaderOverride.name : null;

				await sceneImporter.LoadSceneAsync(onLoadComplete:LoadCompleteAction);

				// Override the shaders on all materials if a shader is provided
				if (shaderOverride != null)
				{
					Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
					foreach (Renderer renderer in renderers)
					{
						renderer.sharedMaterial.shader = shaderOverride;
					}
				}

				LastLoadedScene = sceneImporter.LastLoadedScene;

#if UNITY_ANIMATION
				Animations = sceneImporter.LastLoadedScene.GetComponents<Animation>();

				if (PlayAnimationOnLoad && Animations.Any())
				{
					Animations.First().Play();
				}
#endif
			}
			finally
			{
				if(importOptions.DataLoader != null)
				{
					sceneImporter?.Dispose();
					sceneImporter = null;
					importOptions.DataLoader = null;
				}
			}
		}

		private void LoadCompleteAction(GameObject obj, ExceptionDispatchInfo exceptionDispatchInfo)
		{
			onLoadComplete?.Invoke();
		}
	}
}
