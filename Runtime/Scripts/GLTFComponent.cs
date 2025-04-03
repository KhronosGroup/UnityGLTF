using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityGLTF.Loader;
using UnityGLTF.Plugins;
#if WINDOWS_UWP 
using System; 
#endif

namespace UnityGLTF
{
    /// <summary>
    /// Component to load a GLTF scene with
    /// </summary>
    public class GLTFComponent : MonoBehaviour
	{
		public string GLTFUri = null;
		public bool Multithreaded = true;
		[FormerlySerializedAs("AppendStreamingAssets")] 
		public bool LoadFromStreamingAssets = true;
		public bool PlayAnimationOnLoad = true;
		[Tooltip("Hide the scene object during load, then activate it when complete")]
		public bool HideSceneObjDuringLoad = false;
        public ImporterFactory Factory = null;
        public UnityAction onLoadComplete;

#if UNITY_ANIMATION
        public IEnumerable<Animation> Animations { get; private set; }
#endif

		public bool loadOnStart = true;

		[SerializeField] private int RetryCount = 10;
		[SerializeField] private float RetryTimeout = 2.0f;
		private int numRetries = 0;
		
		public int MaximumLod = 300;
		public int Timeout = 8;
		public GLTFSceneImporter.ColliderType Collider = GLTFSceneImporter.ColliderType.None;
		public GameObject LastLoadedScene { get; private set; } = null;

		[SerializeField]
		private Shader shaderOverride = null;
		
		public Shader ShaderOverride
		{
			get => shaderOverride;
			set
			{
				shaderOverride = value;
				ApplyOverrideShader();
			}
		}

		[Header("Import Settings")]
		public RuntimeTextureCompression TextureCompression = RuntimeTextureCompression.None;
		public GLTFImporterNormals ImportNormals = GLTFImporterNormals.Import;
		public GLTFImporterNormals ImportTangents = GLTFImporterNormals.Import;
		public bool SwapUVs = false;
		[Tooltip("Blend shape frame weight import multiplier. Default is 1. For compatibility with some FBX animations you may need to use 100.")]
		public BlendShapeFrameWeightSetting blendShapeFrameWeight = new BlendShapeFrameWeightSetting(BlendShapeFrameWeightSetting.MultiplierOption.Multiplier1);
		[Tooltip("When enabled, the CPU copy of the mesh will be kept in memory after the mesh has been uploaded to the GPU. This is useful if you want to modify the mesh at runtime.")]
		public bool KeepCPUCopyOfMesh = true;
		[Tooltip("When enabled, the CPU copy of the texture will be kept in memory after the texture has been uploaded to the GPU. This is useful if you want to modify the texture at runtime.")]
		public bool KeepCPUCopyOfTexture = true;
		
		
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
				AsyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ?? gameObject.AddComponent<AsyncCoroutineHelper>(),
				ImportNormals = ImportNormals,
				ImportTangents = ImportTangents,
				SwapUVs = SwapUVs,
				RuntimeTextureCompression = TextureCompression,
			};
			
			var settings = GLTFSettings.GetOrCreateSettings();
			importOptions.ImportContext = new GLTFImportContext(settings);

			GLTFSceneImporter sceneImporter = null;
			try
			{
				if (!Factory) Factory = ScriptableObject.CreateInstance<DefaultImporterFactory>();

                string fullPath;
                var isWebRequest = GLTFUri.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || GLTFUri.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase);
                if (LoadFromStreamingAssets && !isWebRequest)
	                fullPath = Path.Combine(Application.streamingAssetsPath, GLTFUri.TrimStart(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                else
	                fullPath = GLTFUri;

                string dir = URIHelper.GetDirectoryName(fullPath);
                importOptions.DataLoader = new UnityWebRequestLoader(dir);
                sceneImporter = Factory.CreateSceneImporter(
	                Path.GetFileName(fullPath),
	                importOptions
                );

                sceneImporter.SceneParent = gameObject.transform;
				sceneImporter.Collider = Collider;
				sceneImporter.MaximumLod = MaximumLod;
				sceneImporter.Timeout = Timeout;
				sceneImporter.IsMultithreaded = Multithreaded;
				sceneImporter.CustomShaderName = shaderOverride ? shaderOverride.name : null;
				sceneImporter.KeepCPUCopyOfTexture = KeepCPUCopyOfTexture;
				sceneImporter.KeepCPUCopyOfMesh = KeepCPUCopyOfMesh;
				
				// for logging progress
				await sceneImporter.LoadSceneAsync(
					showSceneObj:!HideSceneObjDuringLoad,
					onLoadComplete:LoadCompleteAction
					// ,progress: new Progress<ImportProgress>(
					// 	p =>
					// 	{
					// 		Debug.Log("Progress: " + p);
					// 	})
				);

				// Override the shaders on all materials if a shader is provided
				ApplyOverrideShader();

				LastLoadedScene = sceneImporter.LastLoadedScene;

				if (HideSceneObjDuringLoad && LastLoadedScene)
				{
					LastLoadedScene.SetActive(true);
				}

#if UNITY_ANIMATION
				if (LastLoadedScene)
				{
					Animations = LastLoadedScene.GetComponents<Animation>();
				}
				else
				{
					Animations = System.Array.Empty<Animation>();
				}
				

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
		
		public void ApplyOverrideShader()
		{
			if (shaderOverride != null)
			{
				Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in renderers)
				{
					renderer.sharedMaterial.shader = shaderOverride;
				}
			}
		}

		private void LoadCompleteAction(GameObject obj, ExceptionDispatchInfo exceptionDispatchInfo)
		{
			onLoadComplete?.Invoke();
		}
	}
}
