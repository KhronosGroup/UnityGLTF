using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF.Cache;
using UnityGLTF.Extensions;
using UnityEditor;

namespace UnityGLTF
{
	/// <summary>
	/// Editor windows to load a GLTF scene in editor
	/// </summary>
	///

	public class GLTFEditorImporter
	{
		// Public
		public bool _useGLTFMaterial = false;
		bool _isDone = false;

		// Import paths
		private string _projectDirectoryPath;
		private string _gltfDirectoryPath;
		private string _glTFPath = "";

		// GLTF data
		private byte[] _glTFData;
		protected GLTFRoot _root;
		protected Dictionary<MaterialType, Shader> _shaderCache = new Dictionary<MaterialType, Shader>();
		AssetManager _assetManager = null;
		private int _nbParsedNodes;

		protected AssetCache _assetCache;

		// Import state
		List<string> _messages;
		private string _status = "";
		private TaskManager _taskManager;
		private bool _userStopped = false;

		//Debug only
		private List<string> _importedFiles;
		public UnityEngine.Material defaultMaterial;

		private string _currentSampleName = "";
		public int MAX_VERTICES = 65535;

		public delegate void RefreshWindow();
		private RefreshWindow _refreshMethod;
		private List<string> _assetsToRemove;

		public enum MaterialType
		{
			PbrMetallicRoughness,
			PbrSpecularGlossiness,
			CommonConstant,
			CommonPhong,
			CommonBlinn,
			CommonLambert
		}

		protected const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

		/// <summary>
		/// Constructor
		/// </summary>
		public GLTFEditorImporter()
		{
			Initialize();
		}

		/// <summary>
		/// Constructors setting the delegate function to call after each iteration
		/// </summary>
		/// <param name="delegateFunction">The function to call after each iteration (usually Repaint())</param>
		public GLTFEditorImporter(RefreshWindow delegateFunction)
		{
			_refreshMethod = delegateFunction;
			Initialize();
		}

		/// <summary>
		/// Initializes all the structures and objects
		/// </summary>
		public void Initialize()
		{
			_importedFiles = new List<string>();
			_isDone = true;
			_taskManager = new TaskManager();
			_assetsToRemove = new List<string>();
			_shaderCache.Clear();
			_shaderCache.Add(GLTFEditorImporter.MaterialType.PbrMetallicRoughness, Shader.Find("GLTF/GLTFStandard"));
			_shaderCache.Add(GLTFEditorImporter.MaterialType.CommonConstant, Shader.Find("GLTF/GLTFConstant"));
			defaultMaterial = new UnityEngine.Material(Shader.Find("Standard"));
			_messages = new List<string>();
		}

		/// <summary>
		/// Setup importer for an import
		/// </summary>
		/// <param name="gltfPath">Absolute path to the glTF file to import</param>
		/// <param name="importPath">Path in current project where to import the model</param>
		/// <param name="modelName">Name of the model prefab to create<param>
		public void setupForPath(string gltfPath, string importPath, string modelName)
		{
			_glTFPath = gltfPath;
			_gltfDirectoryPath = Path.GetDirectoryName(_glTFPath);
			_currentSampleName = modelName;
			_projectDirectoryPath = importPath;
			_assetManager = new AssetManager(_projectDirectoryPath);
		}

		/// <summary>
		/// Call this to abort current import
		/// </summary>
		public void abortImport()
		{
			if(!_isDone)
			{
				_userStopped = true;
			}
		}

		/// <summary>
		/// Cleans all generated files and structures
		/// </summary>
		/// 
		public void softClean()
		{
			_messages.Clear();
			if (_assetManager != null)
				_assetManager.softClean();

			_taskManager.clear();
			Resources.UnloadUnusedAssets();
		}

		public void ClearEverything()
		{
			_messages.Clear();

			if (_assetManager != null)
				_assetManager.hardClean();

			for (int i = 0; i < _importedFiles.Count; ++i)
			{
				File.Delete(_importedFiles[i]);
			}

			_taskManager.clear();
			Resources.UnloadUnusedAssets();
		}

		public void Update()
		{
			if(!_isDone)
			{
				if (_userStopped)
				{
					_userStopped = false;
					ClearEverything();
					setStatus("Used interrupted");
					_isDone = true;
				}
				else
				{
					if (_taskManager != null && _taskManager.play())
						_refreshMethod();
					else
					{
						_isDone = true;
						_assetManager.softClean();
						_assetManager.addModelToScene();
					}
				}
			}
		}

		public void OnDestroy()
		{
			ClearEverything();
		}

		private void setStatus(string status, bool last=false)
		{
			if(last)
			{
				_messages[_messages.Count - 1] = status;
			}
			else
			{
				_messages.Add(status);
			}

			_status = "";

			for (int i = 0; i < _messages.Count; ++i)
			{
				_status = _status + "\n" + _messages[i];
			}

			this._refreshMethod();
		}

		public string getStatus()
		{
			return _status;
		}

		public void Load(bool useGLTFMaterial=false)
		{
			_isDone = false;
			_userStopped = false;
			_useGLTFMaterial = useGLTFMaterial;
			_messages.Clear();
			LoadFile();
			LoadGLTFScene();
		}

		// Private
		private void checkValidity()
		{
			if (_importedFiles == null)
			{
				_importedFiles = new List<string>();
			}

			if (_taskManager == null)
			{
				_taskManager = new TaskManager();
			}

			if (_messages == null)
			{
				_messages = new List<string>();
			}
		}

		private void LoadFile(int sceneIndex = -1)
		{
			_glTFData = File.ReadAllBytes(_glTFPath);
			setStatus("Loaded file: " + _glTFPath);
			try
			{
				GLTFProperty.RegisterExtension(new KHR_materials_pbrSpecularGlossinessExtensionFactory());
			}
			catch (Exception)
			{

			}

			_root = GLTFParser.ParseJson(_glTFData);
		}

		private void LoadGLTFScene(int sceneIndex = -1)
		{
			Scene scene;
			if (sceneIndex >= 0 && sceneIndex < _root.Scenes.Count)
			{
				scene = _root.Scenes[sceneIndex];
			}
			else
			{
				scene = _root.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}

			_assetCache = new AssetCache(
				_root.Images != null ? _root.Images.Count : 0,
				_root.Textures != null ? _root.Textures.Count : 0,
				_root.Materials != null ? _root.Materials.Count : 0,
				_root.Buffers != null ? _root.Buffers.Count : 0,
				_root.Meshes != null ? _root.Meshes.Count : 0
			);

			if (_root.Textures != null && _root.Images != null && _root.Textures.Count > _root.Images.Count)
			{
				Debug.LogError("More textures than images");
			}

			// Load dependencies
			LoadBuffersEnum();
			if (_root.Images != null)
				LoadImagesEnum();
			if (_root.Textures != null)
				LoadTexturesEnum();
			if (_root.Materials != null)
				LoadMaterialsEnum();
			LoadMeshesEnum();
			LoadSceneEnum();
		}

		private void LoadBuffersEnum()
		{
			_taskManager.addTask(LoadBuffers());
		}

		private void LoadImagesEnum()
		{
			_taskManager.addTask(LoadImages());
		}

		private void LoadTexturesEnum()
		{
			_taskManager.addTask(LoadTextures());
		}

		private void LoadMaterialsEnum()
		{
			_taskManager.addTask(LoadMaterials());
		}

		private void LoadMeshesEnum()
		{
			_taskManager.addTask(LoadMeshes());
		}
			private void LoadSceneEnum()
		{
			_taskManager.addTask(LoadScene());
		}

		private IEnumerator LoadBuffers()
		{
			if (_root.Buffers != null)
			{
				// todo add fuzzing to verify that buffers are before uri
				for (int i = 0; i < _root.Buffers.Count; ++i)
				{
					GLTF.Schema.Buffer buffer = _root.Buffers[i];
					if (buffer.Uri != null)
					{
						LoadBuffer(_gltfDirectoryPath, buffer, i);
						setStatus("Loaded buffer from file " + buffer.Uri);
					}
					else //null buffer uri indicates GLB buffer loading
					{
						byte[] glbBuffer;
						GLTFParser.ExtractBinaryChunk(_glTFData, i, out glbBuffer);
						_assetCache.BufferCache[i] = glbBuffer;
						setStatus("Loaded embedded buffer " + i);
					}
					yield return null;
				}
			}
		}

		protected virtual void LoadBuffer(string sourceUri, GLTF.Schema.Buffer buffer, int bufferIndex)
		{
			if (buffer.Uri != null)
			{
				byte[] bufferData = null;
				var uri = buffer.Uri;
				var bufferPath = Path.Combine(sourceUri, uri);
				bufferData = File.ReadAllBytes(bufferPath);
				_assetCache.BufferCache[bufferIndex] = bufferData;
			}
		}

		private GameObject createGameObject(string name)
		{
			return _assetManager.createGameObject(name);
		}

		private IEnumerator LoadImages()
		{
			for (int i = 0; i < _root.Images.Count; ++i)
			{
				Image image = _root.Images[i];
				LoadImage(_gltfDirectoryPath, image, i);
				setStatus("Loaded Image " + (i + 1) + "/" + _root.Images.Count + (image.Uri != null ? "(" + image.Uri + ")" : " (embedded)"), i != 0);
				yield return null;
			}
		}

		private void LoadImage(string rootPath, Image image, int imageID)
		{
			if (_assetCache.ImageCache[imageID] == null)
			{
				if (image.Uri != null)
				{
					// Is base64 uri ?
					var uri = image.Uri;

					Regex regex = new Regex(Base64StringInitializer);
					Match match = regex.Match(uri);
					if (match.Success)
					{
						var base64Data = uri.Substring(match.Length);
						var textureData = Convert.FromBase64String(base64Data);

						_assetManager.registerImageFromData(textureData, imageID);
					}
					else if(File.Exists(Path.Combine(rootPath, uri))) // File is a real file
					{
						string imagePath = Path.Combine(rootPath, uri);
						_assetManager.copyAndRegisterImageInProject(imagePath, imageID);
					}
					else
					{
						Debug.Log("Image not found / Unknown image buffer");
					}
				}
				else
				{
					var bufferView = image.BufferView.Value;
					var buffer = bufferView.Buffer.Value;
					var data = new byte[bufferView.ByteLength];

					var bufferContents = _assetCache.BufferCache[bufferView.Buffer.Id];
					System.Buffer.BlockCopy(bufferContents, bufferView.ByteOffset, data, 0, data.Length);
					_assetManager.registerImageFromData(data, imageID);
				}
			}
		}

		private IEnumerator LoadTextures()
		{
			for(int i = 0; i < _root.Textures.Count; ++i)
			{
				SetupTexture(_root.Textures[i], i);
				setStatus("Loaded texture " + (i + 1) + "/" + _root.Textures.Count, i != 0);
				yield return null;
			}
		}

		private void SetupTexture(GLTF.Schema.Texture def, int textureIndex)
		{
			Texture2D source = _assetManager.getOrCreateTexture(def.Source.Id, textureIndex);
			// Default values
			var desiredFilterMode = FilterMode.Bilinear;
			var desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;

			if (def.Sampler != null)
			{
				var sampler = def.Sampler.Value;
				switch (sampler.MinFilter)
				{
					case MinFilterMode.Nearest:
						desiredFilterMode = FilterMode.Point;
						break;
					case MinFilterMode.Linear:
					default:
						desiredFilterMode = FilterMode.Bilinear;
						break;
				}

				switch (sampler.WrapS)
				{
					case GLTF.Schema.WrapMode.ClampToEdge:
						desiredWrapMode = UnityEngine.TextureWrapMode.Clamp;
						break;
					case GLTF.Schema.WrapMode.Repeat:
					default:
						desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;
						break;
				}
			}

			source.filterMode = desiredFilterMode;
			source.wrapMode = desiredWrapMode;
			_assetManager.registerTexture(source);
		}

		private IEnumerator LoadMaterials()
		{
			for(int i = 0; i < _root.Materials.Count; ++i)
			{
				if (_useGLTFMaterial)
					CreateMaterial(_root.Materials[i], i);
				else
					CreateUnityMaterial(_root.Materials[i], i);

				setStatus("Loaded material " + (i + 1) + "/" + _root.Materials.Count, i != 0);
				yield return null;
			}
		}

		protected virtual void CreateUnityMaterial(GLTF.Schema.Material def, int materialIndex)
		{

			Extension specularGlossinessExtension = null;
			bool isSpecularPBR = def.Extensions != null && def.Extensions.TryGetValue("KHR_materials_pbrSpecularGlossiness", out specularGlossinessExtension);

			Shader shader = isSpecularPBR ? Shader.Find("Standard (Specular setup)") : Shader.Find("Standard");

			var material = new UnityEngine.Material(shader);
			material.hideFlags = HideFlags.DontUnloadUnusedAsset;

			material.name = def.Name;
			if (def.AlphaMode == AlphaMode.MASK)
			{
				GLTFUtils.SetupMaterialWithBlendMode(material, GLTFUtils.BlendMode.Cutout);
				material.SetFloat("_Mode", 1);
				material.SetFloat("_Cutoff", (float)def.AlphaCutoff);
			}
			else if (def.AlphaMode == AlphaMode.BLEND)
			{
				GLTFUtils.SetupMaterialWithBlendMode(material, GLTFUtils.BlendMode.Fade);
				material.SetFloat("_Mode", 3);
			}

			if (def.NormalTexture != null)
			{
				var texture = def.NormalTexture.Index.Id;
				Texture2D normalTexture = getTexture(texture) as Texture2D;

				//Automatically set it to normal map
				TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normalTexture)) as TextureImporter;
				im.textureType = TextureImporterType.NormalMap;
				im.SaveAndReimport();
				material.SetTexture("_BumpMap", getTexture(texture));
				material.SetFloat("_BumpScale", (float)def.NormalTexture.Scale);
			}

			if (def.EmissiveTexture != null)
			{
				material.EnableKeyword("EMISSION_MAP_ON");
				var texture = def.EmissiveTexture.Index.Id;
				material.SetTexture("_EmissionMap", getTexture(texture));
				material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor());

			if (specularGlossinessExtension != null)
			{
				KHR_materials_pbrSpecularGlossinessExtension pbr = (KHR_materials_pbrSpecularGlossinessExtension)specularGlossinessExtension;
				material.SetColor("_Color", pbr.DiffuseFactor.ToUnityColor());
				if (pbr.DiffuseTexture != null)
				{
					var texture = pbr.DiffuseTexture.Index.Id;
					material.SetTexture("_MainTex", getTexture(texture));
				}

				if (pbr.SpecularGlossinessTexture != null)
				{
					var texture = pbr.SpecularGlossinessTexture.Index.Id;
					material.SetTexture("_SpecGlossMap", getTexture(texture));
					material.SetFloat("_GlossMapScale", (float)pbr.GlossinessFactor);
					material.SetFloat("_Glossiness", (float)pbr.GlossinessFactor);
				}
				else
				{
					material.SetFloat("_Glossiness", (float)pbr.GlossinessFactor);
				}
				Vector3 specularVec3 = pbr.SpecularFactor.ToUnityVector3();
				material.SetColor("_SpecColor", new Color(specularVec3.x, specularVec3.y, specularVec3.z, 1.0f));

				if (def.OcclusionTexture != null)
				{
					var texture = def.OcclusionTexture.Index.Id;
					material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
					material.SetTexture("_OcclusionMap", getTexture(texture));
				}

				GLTFUtils.SetMaterialKeywords(material, GLTFUtils.WorkflowMode.Specular);
			}
			else if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColor());
				if (pbr.BaseColorTexture != null)
				{
					var texture = pbr.BaseColorTexture.Index.Id;
					material.SetTexture("_MainTex", getTexture(texture));
				}

				material.SetFloat("_Metallic", (float)pbr.MetallicFactor);
				material.SetFloat("_Glossiness", 1.0f - (float)pbr.RoughnessFactor);

				if (pbr.MetallicRoughnessTexture != null)
				{
					var texture = pbr.MetallicRoughnessTexture.Index.Id;
					UnityEngine.Texture2D inputTexture = getTexture(texture) as Texture2D;
					List<Texture2D> splitTextures = splitMetalRoughTexture(inputTexture, def.OcclusionTexture != null);
					material.SetTexture("_MetallicGlossMap", splitTextures[0]);

					if (def.OcclusionTexture != null)
					{
						material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
						material.SetTexture("_OcclusionMap", splitTextures[1]);
					}
				}

				GLTFUtils.SetMaterialKeywords(material, GLTFUtils.WorkflowMode.Metallic);
			}

			material = _assetManager.saveMaterial(material, materialIndex);
			_assetManager._parsedMaterials.Add(material);
			material.hideFlags = HideFlags.None;
		}

		public List<UnityEngine.Texture2D> splitMetalRoughTexture(Texture2D inputTexture, bool hasOcclusion)
		{
			string inputTexturePath = AssetDatabase.GetAssetPath(inputTexture);
			if (!_assetsToRemove.Contains(inputTexturePath))
			{
				_assetsToRemove.Add(inputTexturePath);
			}

			List<UnityEngine.Texture2D> outputs = new List<UnityEngine.Texture2D>();
#if true
			int width = inputTexture.width;
			int height = inputTexture.height;

			Color[] occlusion = new Color[width * height];
			Color[] metalRough = new Color[width * height];
			Color[] textureColors = new Color[width * height];

			GLTFUtils.getPixelsFromTexture(ref inputTexture, out textureColors);

			for (int i = 0; i < height; ++i)
			{
				for (int j = 0; j < width; ++j)
				{
					float occ = textureColors[i * width + j].r;
					float rough = textureColors[i * width + j].g;
					float met = textureColors[i * width + j].b;

					occlusion[i * width + j] = new Color(occ, occ, occ, 1.0f);
					metalRough[i * width + j] = new Color(met, met, met, 1.0f - rough);
				}
			}

			Texture2D metalRoughTexture = new Texture2D(width, height, TextureFormat.ARGB32, true);
			metalRoughTexture.name = Path.GetFileNameWithoutExtension(inputTexturePath) + "_metal";
			metalRoughTexture.SetPixels(metalRough);
			metalRoughTexture.Apply();

			outputs.Add(_assetManager.saveTexture(metalRoughTexture));

			if (hasOcclusion)
			{
				Texture2D occlusionTexture = new Texture2D(width, height);
				occlusionTexture.name = Path.GetFileNameWithoutExtension(inputTexturePath) + "_occlusion";
				occlusionTexture.SetPixels(occlusion);
				occlusionTexture.Apply();

				outputs.Add(_assetManager.saveTexture(occlusionTexture));
			}

			// Delete original texture
			AssetDatabase.Refresh();
#else
			string inputTextureName = Path.GetFileNameWithoutExtension(inputTexturePath);
			string metalRoughPath = Path.Combine(_assetManager.getImportTextureDir(), inputTextureName + "_metal.png");
			GLTFTextureUtils.extractMetalRough(inputTexture, metalRoughPath);
			outputs.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GLTFUtils.getPathProjectFromAbsolute(metalRoughPath)));
			if (hasOcclusion)
			{
				string occlusionPath = Path.Combine(_assetManager.getImportTextureDir(), inputTextureName + "_occlusion.png");
				GLTFTextureUtils.extractOcclusion(inputTexture, occlusionPath);
				outputs.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(GLTFUtils.getPathProjectFromAbsolute(occlusionPath)));
			}

			
#endif
			return outputs;
		}

		protected virtual void CreateMaterial(GLTF.Schema.Material def, int materialIndex)
		{
			Shader shader;

			// get the shader to use for this material
			try
			{
				if (def.PbrMetallicRoughness != null)
					shader = _shaderCache[GLTFEditorImporter.MaterialType.PbrMetallicRoughness];
				else if (_root.ExtensionsUsed != null && _root.ExtensionsUsed.Contains("KHR_materials_common")
						 && def.CommonConstant != null)
					shader = _shaderCache[GLTFEditorImporter.MaterialType.CommonConstant];
				else
					shader = _shaderCache[GLTFEditorImporter.MaterialType.PbrMetallicRoughness];
			}
			catch (KeyNotFoundException)
			{
				Debug.LogWarningFormat("No shader supplied for type of glTF material {0}, using Standard fallback", def.Name);
				shader = Shader.Find("Standard");
			}

			//shader.maximumLOD = MaximumLod;

			var material = new UnityEngine.Material(Shader.Find("GLTF/GLTFStandard"));
			material = _assetManager.saveMaterial(material, materialIndex);

			if (def.AlphaMode == AlphaMode.MASK)
			{
				material.SetOverrideTag("RenderType", "TransparentCutout");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
				material.SetFloat("_Cutoff", (float)def.AlphaCutoff);
			}
			else if (def.AlphaMode == AlphaMode.BLEND)
			{
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			}
			else
			{
				material.SetOverrideTag("RenderType", "Opaque");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
			}

			if (def.DoubleSided)
			{
				material.SetInt("_Cull", (int)CullMode.Off);
			}
			else
			{
				material.SetInt("_Cull", (int)CullMode.Back);
			}

			if (def.PbrMetallicRoughness != null)
			{
				var pbr = def.PbrMetallicRoughness;

				material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColor());

				if (pbr.BaseColorTexture != null)
				{
					var texture = pbr.BaseColorTexture.Index.Id;
					material.SetTexture("_MainTex", getTexture(texture));
				}

				material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

				if (pbr.MetallicRoughnessTexture != null)
				{
					var texture = pbr.MetallicRoughnessTexture.Index.Id;
					material.SetTexture("_MetallicRoughnessMap", getTexture(texture));
				}

				material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
			}

			if (def.CommonConstant != null)
			{
				material.SetColor("_AmbientFactor", def.CommonConstant.AmbientFactor.ToUnityColor());

				if (def.CommonConstant.LightmapTexture != null)
				{
					material.EnableKeyword("LIGHTMAP_ON");

					var texture = def.CommonConstant.LightmapTexture.Index.Id;
					material.SetTexture("_LightMap", getTexture(texture));
					material.SetInt("_LightUV", def.CommonConstant.LightmapTexture.TexCoord);
				}

				material.SetColor("_LightFactor", def.CommonConstant.LightmapFactor.ToUnityColor());
			}

			if (def.NormalTexture != null)
			{
				var textureIndex = def.NormalTexture.Index.Id;
				_assetManager.setTextureNormalMap(textureIndex);
				Texture2D texture = getTexture(textureIndex);
				material.SetTexture("_BumpMap", texture);
				material.SetFloat("_BumpScale", (float)def.NormalTexture.Scale);
			}

			if (def.OcclusionTexture != null)
			{
				var texture = def.OcclusionTexture.Index;

				material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);

				if (def.PbrMetallicRoughness != null
					&& def.PbrMetallicRoughness.MetallicRoughnessTexture != null
					&& def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == texture.Id)
				{
					material.EnableKeyword("OCC_METAL_ROUGH_ON");
				}
				else
				{
					material.SetTexture("_OcclusionMap", getTexture(texture.Id));
				}
			}

			if (def.EmissiveTexture != null)
			{
				var texture = def.EmissiveTexture.Index.Id;
				material.EnableKeyword("EMISSION_MAP_ON");
				material.SetTexture("_EmissionMap", getTexture(texture));
				material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor());
			_assetManager._parsedMaterials.Add(material);
		}

		private Texture2D getTexture(int index)
		{
			return _assetManager.getTexture(index);
		}

		private UnityEngine.Material getMaterial(int index)
		{
			return _assetManager.getMaterial(index);
		}

		private IEnumerator LoadMeshes()
		{
			for(int i = 0; i < _root.Meshes.Count; ++i)
			{
				CreateMeshObject(_root.Meshes[i], i);
				setStatus("Loaded mesh " + (i + 1) + "/" + _root.Meshes.Count, i != 0);
				yield return null;
			}
		}

		protected virtual void CreateMeshObject(GLTF.Schema.Mesh mesh, int meshId)
		{
			for (int i = 0; i < mesh.Primitives.Count; ++i)
			{
				var primitive = mesh.Primitives[i];
				CreateMeshPrimitive(primitive, mesh.Name, meshId, i); // Converted to mesh
			}
		}

		protected virtual void CreateMeshPrimitive(MeshPrimitive primitive, string meshName, int meshID, int primitiveIndex)
		{
			var meshAttributes = BuildMeshAttributes(primitive, meshID, primitiveIndex);
			var vertexCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;

			UnityEngine.Mesh mesh = new UnityEngine.Mesh
			{
				vertices = primitive.Attributes.ContainsKey(SemanticProperties.POSITION)
					? meshAttributes[SemanticProperties.POSITION].AccessorContent.AsVertices.ToUnityVector3()
					: null,
				normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
					? meshAttributes[SemanticProperties.NORMAL].AccessorContent.AsNormals.ToUnityVector3()
					: null,

				uv = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
					? meshAttributes[SemanticProperties.TexCoord(0)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
					? meshAttributes[SemanticProperties.TexCoord(1)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
					? meshAttributes[SemanticProperties.TexCoord(2)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
					? meshAttributes[SemanticProperties.TexCoord(3)].AccessorContent.AsTexcoords.ToUnityVector2()
					: null,

				colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
					? meshAttributes[SemanticProperties.Color(0)].AccessorContent.AsColors.ToUnityColor()
					: null,

				triangles = primitive.Indices != null
					? meshAttributes[SemanticProperties.INDICES].AccessorContent.AsTriangles
					: MeshPrimitive.GenerateTriangles(vertexCount),

				tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
					? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4()
					: null
			};
			mesh.RecalculateBounds();
			mesh.RecalculateTangents();
			mesh = _assetManager.saveMesh(mesh, meshName + "_" + meshID + "_" + primitiveIndex);
			UnityEngine.Material material = primitive.Material != null && primitive.Material.Id >= 0 ? getMaterial(primitive.Material.Id) : defaultMaterial;

			_assetManager.addPrimitiveMeshData(meshID, primitiveIndex, mesh, material);
		}

		protected virtual Dictionary<string, AttributeAccessor> BuildMeshAttributes(MeshPrimitive primitive, int meshID, int primitiveIndex)
		{
			Dictionary<string, AttributeAccessor> attributeAccessors = new Dictionary<string, AttributeAccessor>(primitive.Attributes.Count + 1);
			foreach (var attributePair in primitive.Attributes)
			{
				AttributeAccessor AttributeAccessor = new AttributeAccessor()
				{
					AccessorId = attributePair.Value,
					Buffer = _assetCache.BufferCache[attributePair.Value.Value.BufferView.Value.Buffer.Id]
				};

				attributeAccessors[attributePair.Key] = AttributeAccessor;
			}

			if (primitive.Indices != null)
			{
				AttributeAccessor indexBuilder = new AttributeAccessor()
				{
					AccessorId = primitive.Indices,
					Buffer = _assetCache.BufferCache[primitive.Indices.Value.BufferView.Value.Buffer.Id]
				};

				attributeAccessors[SemanticProperties.INDICES] = indexBuilder;
			}

			GLTFHelpers.BuildMeshAttributes(ref attributeAccessors);
			return attributeAccessors;
		}

		private IEnumerator LoadScene(int sceneIndex = -1)
		{
			Scene scene;
			_nbParsedNodes = 0;

			if (sceneIndex >= 0 && sceneIndex < _root.Scenes.Count)
			{
				scene = _root.Scenes[sceneIndex];
			}
			else
			{
				scene = _root.GetDefaultScene();
			}

			if (scene == null)
			{
				throw new Exception("No default scene in gltf file.");
			}

			var sceneObj = createGameObject(_currentSampleName.Length > 0 ? _currentSampleName : "GLTFScene");
			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node.Value);
				nodeObj.transform.SetParent(sceneObj.transform, false);
			}

			_assetManager.savePrefab(sceneObj, _projectDirectoryPath);

			// Select and focus imported object
			GameObject[] obj= new GameObject[1];
			obj[0] = sceneObj;
			Selection.objects = obj;
			EditorApplication.ExecuteMenuItem("Edit/Frame Selected");
			_messages.Clear();

			setStatus("Successfully imported " + _glTFPath);
			yield return null;
		}

    	protected virtual GameObject CreateNode(Node node)
		{
			var nodeObj = createGameObject(node.Name != null && node.Name.Length > 0 ? node.Name : "GLTFNode");
			//nodeObj.hideFlags = HideFlags.HideInHierarchy;

			_nbParsedNodes++;
			setStatus("Parsing node " +  _nbParsedNodes  + " / " +  _root.Nodes.Count, _nbParsedNodes != 1);

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			if (node.Mesh != null)
			{
				// If several primitive, create several nodes and add them as child of this current Node
				MeshFilter meshFilter = nodeObj.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = _assetManager.getMesh(node.Mesh.Id, 0);

				MeshRenderer meshRenderer = nodeObj.AddComponent<MeshRenderer>();
				meshRenderer.material = _assetManager.getMaterial(node.Mesh.Id, 0);

				for(int i = 1; i < _assetManager._parsedMeshData[node.Mesh.Id].Count; ++i)
				{
					GameObject go = createGameObject(node.Name ?? "GLTFNode_" + i );
					MeshFilter mf = go.AddComponent<MeshFilter>();
					mf.sharedMesh = _assetManager.getMesh(node.Mesh.Id, i);
					MeshRenderer mr = go.AddComponent<MeshRenderer>();
					mr.material = _assetManager.getMaterial(node.Mesh.Id, i);
					go.transform.SetParent(nodeObj.transform, false);
				}
			}

			/* TODO: implement camera (probably a flag to disable for VR as well)
			if (camera != null)
			{
				GameObject cameraObj = camera.Value.Create();
				cameraObj.transform.parent = nodeObj.transform;
			}
			*/

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					var childObj = CreateNode(child.Value);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			return nodeObj;
		}
	}
}

class TaskManager
{
	List<IEnumerator> _tasks;
	IEnumerator _current = null;

	public TaskManager()
	{
		_tasks = new List<IEnumerator>();
	}

	public void addTask(IEnumerator task)
	{
		_tasks.Add(task);
	}

	public void clear()
	{
		_tasks.Clear();
	}

	public bool play()
	{
		if(_tasks.Count > 0)
		{
			if (_current == null || !_current.MoveNext())
			{
				_current = _tasks[0];
				_tasks.RemoveAt(0);
			}
		}

		if (_current != null)
			_current.MoveNext();

		if (_current != null && !_current.MoveNext() && _tasks.Count == 0)
			return false;

		return true;
	}
}