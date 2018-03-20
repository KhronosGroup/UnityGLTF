using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GLTF;
using GLTF.Schema;
using UnityEngine;
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
		public bool _useGLTFMaterial = false;
		bool _isDone = false;

		// Import paths and options
		private string _projectDirectoryPath;
		private string _gltfDirectoryPath;
		private string _glTFPath = "";
		private bool _addToCurrentScene;

		// GLTF data
		private byte[] _glTFData;
		protected GLTFRoot _root;
		AssetManager _assetManager;
		private int _nbParsedNodes;
		private GameObject _sceneObject;
		public UnityEngine.Material defaultMaterial;

		protected AssetCache _assetCache;
		private TaskManager _taskManager;
		private bool _userStopped = false;
		private string _currentSampleName = "";

		private List<string> _assetsToRemove;
		Dictionary<int, GameObject> _importedObjects;
		Dictionary<int, List<SkinnedMeshRenderer>>_skinIndexToGameObjects;

		// Import Progress
		public enum IMPORT_STEP
		{
			READ_FILE,
			BUFFER,
			IMAGE,
			TEXTURE,
			MATERIAL,
			MESH,
			NODE,
			ANIMATION,
			SKIN
		}

		public delegate void RefreshWindow();
		public delegate void ProgressCallback(IMPORT_STEP step, int current, int total);

		private RefreshWindow _finishCallback;
		private ProgressCallback _progressCallback;

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
		public GLTFEditorImporter(ProgressCallback progressCallback, RefreshWindow finish=null)
		{
			_progressCallback = progressCallback;
			_finishCallback = finish;
			Initialize();
		}

		/// <summary>
		/// Initializes all the structures and objects
		/// </summary>
		public void Initialize()
		{
			_importedObjects = new Dictionary<int, GameObject>();
			_skinIndexToGameObjects = new Dictionary<int, List<SkinnedMeshRenderer>>();
			_isDone = true;
			_taskManager = new TaskManager();
			_assetsToRemove = new List<string>();
			defaultMaterial = new UnityEngine.Material(Shader.Find("Standard"));
		}

		/// <summary>
		/// Setup importer for an import
		/// </summary>
		/// <param name="gltfPath">Absolute path to the glTF file to import</param>
		/// <param name="importPath">Path in current project where to import the model</param>
		/// <param name="modelName">Name of the model prefab to create<param>
		public void setupForPath(string gltfPath, string importPath, string modelName, bool addScene=false)
		{
			_glTFPath = gltfPath;
			_gltfDirectoryPath = Path.GetDirectoryName(_glTFPath);
			_currentSampleName = modelName.Length > 0 ? modelName : "GLTFScene";
			_projectDirectoryPath = importPath;
			_assetManager = new AssetManager(_projectDirectoryPath, _currentSampleName);
			_importedObjects.Clear();
			_addToCurrentScene = addScene;
			_skinIndexToGameObjects.Clear();
		}

		public void Update()
		{
			if(!_isDone)
			{
				if (_userStopped)
				{
					_userStopped = false;
					Clear();
					_isDone = true;
				}
				else
				{
					if (_taskManager != null && _taskManager.play())
					{
						// Do stuff
					}
					else
					{
						_isDone = true;
						finishImport();
					}
				}
			}
		}

		public void Load(bool useGLTFMaterial=false)
		{
			_isDone = false;
			_userStopped = false;
			_useGLTFMaterial = useGLTFMaterial;
			LoadFile();
			LoadGLTFScene();
		}

		// Private
		private void checkValidity()
		{
			if (_taskManager == null)
			{
				_taskManager = new TaskManager();
			}
		}

		private void LoadFile(int sceneIndex = -1)
		{
			_glTFData = File.ReadAllBytes(_glTFPath);
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

			// Load dependencies
			LoadBuffersEnum();
			if (_root.Images != null)
				LoadImagesEnum();
			if (_root.Textures != null)
				SetupTexturesEnum();
			if (_root.Materials != null)
				LoadMaterialsEnum();
			LoadMeshesEnum();
			LoadSceneEnum();

			if (_root.Animations != null && _root.Animations.Count > 0)
				LoadAnimationsEnum();

			if (_root.Skins != null && _root.Skins.Count > 0)
				LoadSkinsEnum();
		}

		private void LoadBuffersEnum()
		{
			_taskManager.addTask(LoadBuffers());
		}

		private void LoadImagesEnum()
		{
			_taskManager.addTask(LoadImages());
		}

		private void SetupTexturesEnum()
		{
			_taskManager.addTask(SetupTextures());
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
		private void LoadAnimationsEnum()
		{
			_taskManager.addTask(LoadAnimations());
		}

		private void LoadSkinsEnum()
		{
			_taskManager.addTask(LoadSkins());
		}

		private IEnumerator LoadBuffers()
		{
			if (_root.Buffers != null)
			{
				// todo add fuzzing to verify that buffers are before uri
				setProgress(IMPORT_STEP.BUFFER, 0, _root.Buffers.Count);
				for (int i = 0; i < _root.Buffers.Count; ++i)
				{
					GLTF.Schema.Buffer buffer = _root.Buffers[i];
					if (buffer.Uri != null)
					{
						LoadBuffer(_gltfDirectoryPath, buffer, i);
					}
					else //null buffer uri indicates GLB buffer loading
					{
						byte[] glbBuffer;
						GLTFParser.ExtractBinaryChunk(_glTFData, i, out glbBuffer);
						_assetCache.BufferCache[i] = glbBuffer;
					}
					setProgress(IMPORT_STEP.BUFFER, (i + 1), _root.Buffers.Count);
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

		private IEnumerator LoadImages()
		{
			for (int i = 0; i < _root.Images.Count; ++i)
			{
				Image image = _root.Images[i];
				LoadImage(_gltfDirectoryPath, image, i);
				setProgress(IMPORT_STEP.IMAGE, (i + 1), _root.Images.Count);
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

		private IEnumerator SetupTextures()
		{
			for(int i = 0; i < _root.Textures.Count; ++i)
			{
				SetupTexture(_root.Textures[i], i);
				setProgress(IMPORT_STEP.TEXTURE, (i + 1), _root.Textures.Count);
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
				CreateUnityMaterial(_root.Materials[i], i);
				setProgress(IMPORT_STEP.MATERIAL, (i + 1), _root.Materials.Count);
				yield return null;
			}
		}

		protected virtual void CreateUnityMaterial(GLTF.Schema.Material def, int materialIndex)
		{
			Extension specularGlossinessExtension = null;
			bool isSpecularPBR = def.Extensions != null && def.Extensions.TryGetValue("KHR_materials_pbrSpecularGlossiness", out specularGlossinessExtension);

			Shader shader = isSpecularPBR ? Shader.Find("Standard (Specular setup)") : Shader.Find("Standard");

			var material = new UnityEngine.Material(shader);
			material.hideFlags = HideFlags.DontUnloadUnusedAsset; // Avoid material to be deleted while being built
			material.name = def.Name;

			//Transparency
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

			// PBR channels
			if (specularGlossinessExtension != null)
			{
				KHR_materials_pbrSpecularGlossinessExtension pbr = (KHR_materials_pbrSpecularGlossinessExtension)specularGlossinessExtension;
				material.SetColor("_Color", pbr.DiffuseFactor.ToUnityColor().gamma);
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

				material.SetColor("_Color", pbr.BaseColorFactor.ToUnityColor().gamma);
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
					List<Texture2D> splitTextures = splitMetalRoughTexture(inputTexture, def.OcclusionTexture != null, (float)pbr.MetallicFactor, (float)pbr.RoughnessFactor);
					material.SetTexture("_MetallicGlossMap", splitTextures[0]);

					if (def.OcclusionTexture != null)
					{
						material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
						material.SetTexture("_OcclusionMap", splitTextures[1]);
					}
				}

				GLTFUtils.SetMaterialKeywords(material, GLTFUtils.WorkflowMode.Metallic);
			}

			material.SetColor("_EmissionColor", def.EmissiveFactor.ToUnityColor().gamma);
			material = _assetManager.saveMaterial(material, materialIndex);
			_assetManager._parsedMaterials.Add(material);
			material.hideFlags = HideFlags.None;
		}

		public List<UnityEngine.Texture2D> splitMetalRoughTexture(Texture2D inputTexture, bool hasOcclusion, float metallicFactor, float roughnessFactor)
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
					metalRough[i * width + j] = new Color(met * metallicFactor, met * metallicFactor, met * metallicFactor, (1.0f - rough) * roughnessFactor);
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
				setProgress(IMPORT_STEP.MESH, (i + 1), _root.Meshes.Count);
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
					? meshAttributes[SemanticProperties.TANGENT].AccessorContent.AsTangents.ToUnityVector4(true)
					: null
			};

			if (primitive.Attributes.ContainsKey(SemanticProperties.JOINT) && primitive.Attributes.ContainsKey(SemanticProperties.WEIGHT))
			{
				Vector4[] bones = new Vector4[1];
				Vector4[] weights = new Vector4[1];

				LoadSkinnedMeshAttributes(meshID, primitiveIndex, ref bones, ref weights);
				if(bones.Length != mesh.vertices.Length || weights.Length != mesh.vertices.Length)
				{
					Debug.LogError("Not enough skinning data (bones:" + bones.Length + " weights:" + weights.Length + "  verts:" + mesh.vertices.Length + ")");
					return;
				}

				BoneWeight[] bws = new BoneWeight[mesh.vertices.Length];
				int maxBonesIndex = 0;
				for (int i = 0; i < bws.Length; ++i)
				{
					// Unity seems expects the the sum of weights to be 1.
					float[] normalizedWeights =  GLTFUtils.normalizeBoneWeights(weights[i]);

					bws[i].boneIndex0 = (int)bones[i].x;
					bws[i].weight0 = normalizedWeights[0];

					bws[i].boneIndex1 = (int)bones[i].y;
					bws[i].weight1 = normalizedWeights[1];

					bws[i].boneIndex2 = (int)bones[i].z;
					bws[i].weight2 = normalizedWeights[2];

					bws[i].boneIndex3 = (int)bones[i].w;
					bws[i].weight3 = normalizedWeights[3];

					maxBonesIndex = (int)Mathf.Max(maxBonesIndex, bones[i].x, bones[i].y, bones[i].z, bones[i].w);
				}

				mesh.boneWeights = bws;

				// initialize inverseBindMatrix array with identity matrix in order to output a valid mesh object
				Matrix4x4[] bindposes = new Matrix4x4[maxBonesIndex];
				for(int j=0; j < maxBonesIndex; ++j)
				{
					bindposes[j] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
				}
				mesh.bindposes = bindposes;
			}

			if(primitive.Targets != null && primitive.Targets.Count > 0)
			{
				for (int b = 0; b < primitive.Targets.Count; ++b)
				{
					Vector3[] deltaVertices = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];
					Vector3[] deltaNormals = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];
					Vector3[] deltaTangents = new Vector3[primitive.Targets[b]["POSITION"].Value.Count];

					if(primitive.Targets[b].ContainsKey("POSITION"))
					{
						NumericArray num = new NumericArray();
						deltaVertices = primitive.Targets[b]["POSITION"].Value.AsVector3Array(ref num, _assetCache.BufferCache[0], false).ToUnityVector3(true);
					}
					if (primitive.Targets[b].ContainsKey("NORMAL"))
					{
						NumericArray num = new NumericArray();
						deltaNormals = primitive.Targets[b]["NORMAL"].Value.AsVector3Array(ref num, _assetCache.BufferCache[0], true).ToUnityVector3(true);
					}
					//if (primitive.Targets[b].ContainsKey("TANGENT"))
					//{
					//	deltaTangents = primitive.Targets[b]["TANGENT"].Value.AsVector3Array(ref num, _assetCache.BufferCache[0], true).ToUnityVector3(true);
					//}

					mesh.AddBlendShapeFrame(GLTFUtils.buildBlendShapeName(meshID, b), 1.0f, deltaVertices, deltaNormals, deltaTangents);
				}
			}

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

			_sceneObject = createGameObject(_currentSampleName);
			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node.Value, node.Id);
				nodeObj.transform.SetParent(_sceneObject.transform, false);
			}

			yield return null;
		}

		private IEnumerator LoadAnimations()
		{
			for (int i = 0; i < _root.Animations.Count; ++i)
			{
				AnimationClip clip = new AnimationClip();
				clip.wrapMode = UnityEngine.WrapMode.Loop;
				LoadAnimation(_root.Animations[i], i, clip);
				setProgress(IMPORT_STEP.ANIMATION, (i + 1), _root.Animations.Count);
				_assetManager.saveAnimationClip(clip);
				yield return null;
			}
		}

		private void LoadAnimation(GLTF.Schema.Animation gltfAnimation, int index, AnimationClip clip)
		{
			clip.name = gltfAnimation.Name != null && gltfAnimation.Name.Length > 0 ? gltfAnimation.Name : "GLTFAnimation_" + index;
			for(int i=0; i < gltfAnimation.Channels.Count; ++i)
			{
				AnimationChannel channel = gltfAnimation.Channels[i];
				addGLTFChannelDataToClip(gltfAnimation.Channels[i], clip);
			}

			clip.EnsureQuaternionContinuity();
		}

		private void addGLTFChannelDataToClip(GLTF.Schema.AnimationChannel channel, AnimationClip clip)
		{
			int animatedNodeIndex = channel.Target.Node.Id;
			if (!_importedObjects.ContainsKey(animatedNodeIndex))
			{
				Debug.Log("Node '" + animatedNodeIndex + "' found for animation, aborting.");
			}

			Transform animatedNode = _importedObjects[animatedNodeIndex].transform;
			string nodePath = AnimationUtility.CalculateTransformPath(animatedNode, _sceneObject.transform);

			bool isStepInterpolation = channel.Sampler.Value.Interpolation != InterpolationType.LINEAR;

			byte[] timeBufferData = _assetCache.BufferCache[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
			float[] times = GLTFHelpers.ParseKeyframeTimes(channel.Sampler.Value.Input.Value, timeBufferData);

			if (channel.Target.Path == GLTFAnimationChannelPath.translation || channel.Target.Path == GLTFAnimationChannelPath.scale)
			{
				byte[] bufferData = _assetCache.BufferCache[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				GLTF.Math.Vector3[] keyValues = GLTFHelpers.ParseVector3Keyframes(channel.Sampler.Value.Output.Value, bufferData);
				if (keyValues == null)
					return;

				Vector3[] values = keyValues.ToUnityVector3();
				AnimationCurve[] vector3Curves = GLTFUtils.createCurvesFromArrays(times, values, isStepInterpolation, channel.Target.Path == GLTFAnimationChannelPath.translation);

				if (channel.Target.Path == GLTFAnimationChannelPath.translation)
					GLTFUtils.addTranslationCurvesToClip(vector3Curves, nodePath, ref clip);
				else
					GLTFUtils.addScaleCurvesToClip(vector3Curves, nodePath, ref clip);
			}
			else if (channel.Target.Path == GLTFAnimationChannelPath.rotation)
			{
				byte[] bufferData = _assetCache.BufferCache[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				Vector4[] values = GLTFHelpers.ParseRotationKeyframes(channel.Sampler.Value.Output.Value, bufferData).ToUnityVector4();
				AnimationCurve[] rotationCurves = GLTFUtils.createCurvesFromArrays(times, values, isStepInterpolation);

				GLTFUtils.addRotationCurvesToClip(rotationCurves, nodePath, ref clip);
			}
			else if(channel.Target.Path == GLTFAnimationChannelPath.weights)
			{
				List<string> morphTargets = new List<string>();
				int meshIndex = _root.Nodes[animatedNodeIndex].Mesh.Id;
				for(int i=0; i<  _root.Meshes[meshIndex].Primitives[0].Targets.Count; ++i)
				{
					morphTargets.Add(GLTFUtils.buildBlendShapeName(meshIndex, i));
				}

				byte[] bufferData = _assetCache.BufferCache[channel.Sampler.Value.Output.Value.BufferView.Value.Buffer.Id];
				float[] values = GLTFHelpers.ParseKeyframeTimes(channel.Sampler.Value.Output.Value, bufferData);
				AnimationCurve[] morphCurves = GLTFUtils.buildMorphAnimationCurves(times, values, morphTargets.Count);

				GLTFUtils.addMorphAnimationCurvesToClip(morphCurves, nodePath, morphTargets.ToArray(), ref clip);
			}
			else
			{
				Debug.Log("Unsupported animation channel target: " + channel.Target.Path);
			}
		}

		private void setProgress(IMPORT_STEP step, int current, int total)
		{
			if (_progressCallback != null)
				_progressCallback(step, current, total);
		}

		private IEnumerator LoadSkins()
		{
			setProgress(IMPORT_STEP.SKIN, 0, _root.Skins.Count);
			for (int i = 0; i < _root.Skins.Count; ++i)
			{
				LoadSkin(_root.Skins[i], i);
				setProgress(IMPORT_STEP.SKIN, (i + 1), _root.Skins.Count);
				yield return null;
			}
		}

		private void LoadSkin(GLTF.Schema.Skin skin, int index)
		{
			Transform[] boneList = new Transform[skin.Joints.Count];
			for (int i = 0; i < skin.Joints.Count; ++i)
			{
				boneList[i] = _importedObjects[skin.Joints[i].Id].transform;
			}

			foreach (SkinnedMeshRenderer skinMesh in _skinIndexToGameObjects[index])
			{
				skinMesh.bones = boneList;
			}
		}

		private void BuildSkinnedMesh(GameObject nodeObj, GLTF.Schema.Skin skin, int meshIndex, int primitiveIndex)
		{
			if(skin.InverseBindMatrices.Value.Count == 0)
				return;

			SkinnedMeshRenderer skinMesh = nodeObj.AddComponent<SkinnedMeshRenderer>();
			skinMesh.sharedMesh = _assetManager.getMesh(meshIndex, primitiveIndex);
			skinMesh.sharedMaterial = _assetManager.getMaterial(meshIndex, primitiveIndex);

			byte[] bufferData = _assetCache.BufferCache[skin.InverseBindMatrices.Value.BufferView.Value.Buffer.Id];
			NumericArray content = new NumericArray();
			List<Matrix4x4> bindPoseMatrices = new List<Matrix4x4>();
			GLTF.Math.Matrix4x4[] inverseBindMatrices = skin.InverseBindMatrices.Value.AsMatrixArray(ref content, bufferData);
			foreach (GLTF.Math.Matrix4x4 mat in inverseBindMatrices)
			{
				bindPoseMatrices.Add(mat.ToUnityMatrix().switchHandedness());
			}

			skinMesh.sharedMesh.bindposes = bindPoseMatrices.ToArray();
			if(skin.Skeleton != null && _importedObjects.ContainsKey(skin.Skeleton.Id))
				skinMesh.rootBone = skin.Skeleton == null ? _importedObjects[skin.Skeleton.Id].transform : null;
		}

		protected virtual void LoadSkinnedMeshAttributes(int meshIndex, int primitiveIndex, ref Vector4[] boneIndexes, ref Vector4[] weights)
		{
			GLTF.Schema.MeshPrimitive prim = _root.Meshes[meshIndex].Primitives[primitiveIndex];
			if (!prim.Attributes.ContainsKey(SemanticProperties.JOINT) || !prim.Attributes.ContainsKey(SemanticProperties.WEIGHT))
				return;

			parseAttribute(ref prim, SemanticProperties.JOINT, ref boneIndexes);
			parseAttribute(ref prim, SemanticProperties.WEIGHT, ref weights);
			foreach(Vector4 wei in weights)
			{
				wei.Normalize();
			}
		}

		private void parseAttribute(ref GLTF.Schema.MeshPrimitive prim, string property, ref Vector4[] values)
		{
			byte[] bufferData = _assetCache.BufferCache[prim.Attributes[property].Value.BufferView.Value.Buffer.Id];
			NumericArray num = new NumericArray();
			GLTF.Math.Vector4[] gltfValues = prim.Attributes[property].Value.AsVector4Array(ref num, bufferData);
			values = new Vector4[gltfValues.Length];

			for (int i = 0; i < gltfValues.Length; ++i)
			{
				values[i] = gltfValues[i].ToUnityVector4();
			}
		}

		protected virtual GameObject CreateNode(Node node, int index)
		{
			var nodeObj = createGameObject(node.Name != null && node.Name.Length > 0 ? node.Name : "GLTFNode_" + index);

			_nbParsedNodes++;
			setProgress(IMPORT_STEP.NODE, _nbParsedNodes, _root.Nodes.Count);
			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			bool isSkinned = node.Skin != null && isValidSkin(node.Skin.Id);
			bool hasMorphOnly = node.Skin == null && node.Mesh != null && node.Mesh.Value.Weights != null && node.Mesh.Value.Weights.Count != 0;
			if (node.Mesh != null)
			{
				if (isSkinned) // Mesh is skinned (it can also have morph)
				{
					if (!_skinIndexToGameObjects.ContainsKey(node.Skin.Id))
						_skinIndexToGameObjects[node.Skin.Id] = new List<SkinnedMeshRenderer>();

					BuildSkinnedMesh(nodeObj, node.Skin.Value, node.Mesh.Id, 0);
					_skinIndexToGameObjects[node.Skin.Id].Add(nodeObj.GetComponent<SkinnedMeshRenderer>());
				}
				else if (hasMorphOnly)
				{
					SkinnedMeshRenderer smr = nodeObj.AddComponent<SkinnedMeshRenderer>();
					smr.sharedMesh = _assetManager.getMesh(node.Mesh.Id, 0);
					smr.sharedMaterial = _assetManager.getMaterial(node.Mesh.Id, 0);
				}
				else
				{
					// If several primitive, create several nodes and add them as child of this current Node
					MeshFilter meshFilter = nodeObj.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = _assetManager.getMesh(node.Mesh.Id, 0);

					MeshRenderer meshRenderer = nodeObj.AddComponent<MeshRenderer>();
					meshRenderer.material = _assetManager.getMaterial(node.Mesh.Id, 0);
				}

				for(int i = 1; i < _assetManager._parsedMeshData[node.Mesh.Id].Count; ++i)
				{
					GameObject go = createGameObject(node.Name ?? "GLTFNode_" + i);
					if (isSkinned)
					{
						BuildSkinnedMesh(go, node.Skin.Value, node.Mesh.Id, i);
						_skinIndexToGameObjects[node.Skin.Id].Add(go.GetComponent<SkinnedMeshRenderer>());
					}
					else if (hasMorphOnly)
					{
						SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
						smr.sharedMesh = _assetManager.getMesh(node.Mesh.Id, i);
						smr.sharedMaterial = _assetManager.getMaterial(node.Mesh.Id, i);
					}
					else
					{
						MeshFilter mf = go.AddComponent<MeshFilter>();
						mf.sharedMesh = _assetManager.getMesh(node.Mesh.Id, i);
						MeshRenderer mr = go.AddComponent<MeshRenderer>();
						mr.material = _assetManager.getMaterial(node.Mesh.Id, i);
					}

					go.transform.SetParent(nodeObj.transform, false);
				}
			}

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					var childObj = CreateNode(child.Value, child.Id);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			_importedObjects.Add(index, nodeObj);
			return nodeObj;
		}

		private GameObject createGameObject(string name)
		{
			name = GLTFUtils.cleanName(name);
			return _assetManager.createGameObject(name);
		}

		private bool isValidSkin(int skinIndex)
		{
			if (skinIndex >= _root.Skins.Count)
				return false;

			Skin glTFSkin = _root.Skins[skinIndex];

			return glTFSkin.Joints.Count > 0 && glTFSkin.Joints.Count == glTFSkin.InverseBindMatrices.Value.Count;
		}

		private void finishImport()
		{
			GameObject prefab = _assetManager.savePrefab(_sceneObject, _projectDirectoryPath, _addToCurrentScene);
			if (_finishCallback != null)
				_finishCallback();

			Clear();

			if (_addToCurrentScene == true)
			{
				// Select and focus imported object
				_sceneObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
				GameObject[] obj = new GameObject[1];
				obj[0] = _sceneObject;
				Selection.objects = obj;
#if UNITY_2017
				EditorApplication.ExecuteMenuItem("Edit/Frame Selected");
#endif
			}
		}

		/// <summary>
		/// Call this to abort current import
		/// </summary>
		public void abortImport()
		{
			if (!_isDone)
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
			if (_assetManager != null)
				_assetManager.softClean();

			_taskManager.clear();
			Resources.UnloadUnusedAssets();
		}

		public void Clear()
		{
			if (_assetManager != null)
				_assetManager.softClean();

			_taskManager.clear();
			Resources.UnloadUnusedAssets();
		}

		private void cleanObjects()
		{
			foreach (GameObject ob in _importedObjects.Values)
			{
				GameObject.DestroyImmediate(ob);
			}
			GameObject.DestroyImmediate(_sceneObject);
			_sceneObject = null;
			_assetManager.softClean();
		}

		public void OnDestroy()
		{
			Clear();
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