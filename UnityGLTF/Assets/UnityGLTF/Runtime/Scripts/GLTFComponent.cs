using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
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

        public IEnumerable<Animation> Animations { get; private set; }

		[SerializeField]
		private bool loadOnStart = true;

		[SerializeField] private bool MaterialsOnly = false;

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
                Factory = Factory ?? ScriptableObject.CreateInstance<DefaultImporterFactory>();

				if (UseStream)
				{
					string fullPath;
					if (AppendStreamingAssets)
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
					string directoryPath = URIHelper.GetDirectoryName(fullPath);
					importOptions.DataLoader = new FileLoader(directoryPath);
					sceneImporter = Factory.CreateSceneImporter(
						Path.GetFileName(GLTFUri),
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

				if (MaterialsOnly)
				{
					var mat = await sceneImporter.LoadMaterialAsync(0);
					var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
					cube.transform.SetParent(gameObject.transform);
					var renderer = cube.GetComponent<Renderer>();
					renderer.sharedMaterial = mat;
				}
				else
				{
					await sceneImporter.LoadSceneAsync();
				}

				// Override the shaders on all materials if a shader is provided
				if (shaderOverride != null)
				{
					Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
					foreach (Renderer renderer in renderers)
					{
						renderer.sharedMaterial.shader = shaderOverride;
					}
				}

				print("model loaded with vertices: " + sceneImporter.Statistics.VertexCount.ToString() + ", triangles: " + sceneImporter.Statistics.TriangleCount.ToString());
				LastLoadedScene = sceneImporter.LastLoadedScene;

				Animations = sceneImporter.LastLoadedScene.GetComponents<Animation>();

				if (PlayAnimationOnLoad && Animations.Any())
				{
					Animations.FirstOrDefault().Play();
				}
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
	}
}
