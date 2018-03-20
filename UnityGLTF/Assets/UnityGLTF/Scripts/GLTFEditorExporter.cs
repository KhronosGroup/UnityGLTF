using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;
using UnityEditor;

namespace UnityGLTF
{
	public class GLTFEditorExporter
	{
		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTF.Schema.Buffer _buffer;
		private BinaryWriter _bufferWriter;
		private List<Texture2D> _images;
		private List<UnityEngine.Texture> _textures;
		private List<UnityEngine.Material> _materials;
		private Dictionary<int, int> _exportedTransforms;
		private List<Transform> _animatedNodes;
		private List<Transform> _skinnedNodes;
		private Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh> _bakedMeshes;

		// Temporary setting to avoid validation issue 'not multiple of 4' in bufferView.offset
		private bool _forceIndicesUint = true;

		private bool _exportAnimation = true;
		private bool _bakeSkinnedMeshes = false;
		private Dictionary<string, string> _exportedFiles;

		protected struct PrimKey
		{
			public UnityEngine.Mesh Mesh;
			public UnityEngine.Material Material;
		}
		private readonly Dictionary<PrimKey, MeshId> _primOwner = new Dictionary<PrimKey, MeshId>();
		private readonly Dictionary<UnityEngine.Mesh, MeshPrimitive[]> _meshToPrims = new Dictionary<UnityEngine.Mesh, MeshPrimitive[]>();

		private GLTFTextureUtilsCache _textureCache;
		public bool ExportNames = true;

		// Progress
		public enum EXPORT_STEP
		{
			NODES,
			ANIMATIONS,
			SKINNING,
			IMAGES
		}
		public delegate void ProgressCallback(EXPORT_STEP step, float current, float total);
		public delegate void FinishCallback();
		ProgressCallback _progressCallback;
		FinishCallback _finishCallback;

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		public GLTFEditorExporter()
		{
			initializeStructures();
		}

		public void setProgressCallback(ProgressCallback cb)
		{
			_progressCallback = cb;
		}

		private void updateProgress(EXPORT_STEP step, float current, float total)
		{
			if(_progressCallback != null)
			{
				_progressCallback(step, current, total);
			}
		}

		public void setExportFinishCallback(FinishCallback cb)
		{
			_finishCallback = cb;
		}

		public void triggerFinishCallback()
		{
			if (_finishCallback != null)
				_finishCallback();

		}
		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		public GLTFEditorExporter(string generator)
		{
			initializeStructures(generator);
		}

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		public GLTFEditorExporter(Transform[] rootTransforms, string generator = "")
		{
			initializeStructures(generator);
			_rootTransforms = rootTransforms;
		}

		public void setTransforms(Transform[] rootTransforms)
		{
			_rootTransforms = rootTransforms;
		}

		public void enableAnimation(bool enable)
		{
			_exportAnimation = enable;
		}

		private void initializeStructures(string generator = "")
		{
			_exportedTransforms = new Dictionary<int, int>();
			_animatedNodes = new List<Transform>();
			_skinnedNodes = new List<Transform>();
			_exportedFiles = new Dictionary<string, string>();
			_textureCache = new GLTFTextureUtilsCache();
			_bakedMeshes = new Dictionary<SkinnedMeshRenderer, UnityEngine.Mesh>();

			_root = new GLTFRoot
			{
				Accessors = new List<Accessor>(),
				Animations = new List<GLTF.Schema.Animation>(),
				Asset = new Asset
				{
					Version = "2.0",
					Generator = generator.Length > 0 ? generator : "UnityGLTF (" + Application.unityVersion + ")"
				},
				Buffers = new List<GLTF.Schema.Buffer>(),
				BufferViews = new List<BufferView>(),
				ExtensionsUsed = new List<string>(),
				ExtensionsRequired = new List<string>(),
				Images = new List<Image>(),
				Materials = new List<GLTF.Schema.Material>(),
				Meshes = new List<GLTF.Schema.Mesh>(),
				Nodes = new List<Node>(),
				Samplers = new List<Sampler>(),
				Scenes = new List<Scene>(),
				Skins = new List<Skin>(),
				Textures = new List<GLTF.Schema.Texture>(),
			};

			_images = new List<Texture2D>();
			_materials = new List<UnityEngine.Material>();
			_textures = new List<UnityEngine.Texture>();

			_buffer = new GLTF.Schema.Buffer();
			_bufferId = new BufferId
			{
				Id = _root.Buffers.Count,
				Root = _root
			};
			_root.Buffers.Add(_buffer);
		}

		/// <summary>
		/// Gets the root object of the exported GLTF
		/// </summary>
		/// <returns>Root parsed GLTF Json</returns>
		public GLTFRoot GetRoot() {
			return _root;
		}

		public Dictionary<string, string> getExportedFilesList()
		{
			return _exportedFiles;
		}

		public void clear()
		{
			initializeStructures();
		}

		/// <summary>
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName)
		{
			string binPath = Path.Combine(path, fileName + ".bin");
			var binFile = File.Create(binPath);
			_bufferWriter = new BinaryWriter(binFile);

			_root.Scene = ExportScene(fileName, _rootTransforms);
			if (_exportAnimation)
			{
				exportAnimation();
				// Export skins
				for (int i = 0; i < _skinnedNodes.Count; ++i)
				{
					Transform t = _skinnedNodes[i];
					exportSkinFromNode(t);

					updateProgress(EXPORT_STEP.SKINNING, i,  _skinnedNodes.Count);
				}
			}


			_buffer.Uri = fileName + ".bin";
			_buffer.ByteLength = (int)_bufferWriter.BaseStream.Length;

			_exportedFiles.Add(binPath, "");

			string gltfPath = Path.Combine(path, fileName + ".gltf");
			var gltfFile = File.CreateText(gltfPath);
			_root.Serialize(gltfFile);

#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
			gltfFile.Close();
			binFile.Close();
#endif
			_exportedFiles.Add(gltfPath, "");
			bool backup = GL.sRGBWrite;
			GL.sRGBWrite = true;
			foreach (var image in _images)
			{
				//Should filter regarding channel that use it
				string outputPath = Path.Combine(path, GLTFUtils.buildImageName(image)); // Png by default, but will be changed in write function
				string finalOutputPath = GLTFTextureUtils.writeTextureOnDisk(_textureCache.flipTexture(image), outputPath, true);
				_exportedFiles.Add(finalOutputPath, "");

				updateProgress(EXPORT_STEP.IMAGES, _images.IndexOf(image), _images.Count);
			}
			GL.sRGBWrite = backup;
			triggerFinishCallback();
		}

		private void exportAnimation()
		{
			GLTF.Schema.Animation anim = new GLTF.Schema.Animation();
			anim.Name = "Take 001";
			for (int i = 0; i < _animatedNodes.Count; ++i)
			{
				Transform t = _animatedNodes[i];
				exportAnimationFromNode(ref t, ref anim);

				updateProgress(EXPORT_STEP.ANIMATIONS, i, _animatedNodes.Count);
			}

			if (anim.Channels.Count > 0 && anim.Samplers.Count > 0)
			{
				_root.Animations.Add(anim);
			}
		}

		private UnityEngine.Mesh getMesh(GameObject gameObject)
		{
			if(gameObject.GetComponent<MeshFilter>())
			{
				return gameObject.GetComponent<MeshFilter>().sharedMesh;
			}

			SkinnedMeshRenderer skinMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
			if (skinMesh)
			{
				if(!_exportAnimation && _bakeSkinnedMeshes)
				{
					if(!_bakedMeshes.ContainsKey(skinMesh))
					{
						UnityEngine.Mesh bakedMesh = new UnityEngine.Mesh();
						skinMesh.BakeMesh(bakedMesh);
						_bakedMeshes.Add(skinMesh, bakedMesh);
					}

					return _bakedMeshes[skinMesh];
				}

				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
			}

			return null;
		}

		private UnityEngine.Material getMaterial(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshRenderer>())
			{
				return gameObject.GetComponent<MeshRenderer>().sharedMaterial;
			}

			if (gameObject.GetComponent<SkinnedMeshRenderer>())
			{
				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
			}

			return null;
		}

		private UnityEngine.Material[] getMaterials(GameObject gameObject)
		{
			if (gameObject.GetComponent<MeshRenderer>())
			{
				return gameObject.GetComponent<MeshRenderer>().sharedMaterials;
			}

			if (gameObject.GetComponent<SkinnedMeshRenderer>())
			{
				return gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			}

			return null;
		}

		private int countNodes(Transform[] trs)
		{
			int ct = 0;
			foreach (Transform tr in trs)
			{
				countRecursive(tr, ref ct);
			}

			return ct;
		}

		private void countRecursive(Transform tr, ref int count)
		{
			count += 1;
			if (tr.childCount > 0)
			{
				for (int i = 0; i < tr.childCount; ++i)
				{
					countRecursive(tr.GetChild(i), ref count);
				}
			}
		}

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			var scene = new Scene();

			if (ExportNames)
			{
				scene.Name = name;
			}
			int nodeCount = countNodes(rootObjTransforms);
			scene.Nodes = new List<NodeId>(rootObjTransforms.Length);
			foreach (var transform in rootObjTransforms)
			{
				scene.Nodes.Add(ExportNode(transform, nodeCount));
			}

			_root.Scenes.Add(scene);

			return new SceneId {
				Id = _root.Scenes.Count - 1,
				Root = _root
			};
		}

		private NodeId ExportNode(Transform nodeTransform, int nodeCount)
		{
			var node = new Node();

			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}
			if(nodeTransform.GetComponent<UnityEngine.Animation>() || nodeTransform.GetComponent<UnityEngine.Animator>())
			{
				_animatedNodes.Add(nodeTransform);
			}
			if(nodeTransform.GetComponent<SkinnedMeshRenderer>())
			{
				_skinnedNodes.Add(nodeTransform);
			}
			// If object is on top of the selection, use global transform
			bool useLocal = !Array.Exists(_rootTransforms, element => element == nodeTransform);
			node.SetUnityTransform(nodeTransform, useLocal);

			var id = new NodeId {
				Id = _root.Nodes.Count,
				Root = _root
			};

			// Register nodes for animation parsing (could be disabled is animation is disables)
			_exportedTransforms.Add(nodeTransform.GetInstanceID(), _root.Nodes.Count);

			_root.Nodes.Add(node);
			// Update progress
			updateProgress(EXPORT_STEP.NODES, _root.Nodes.Count, nodeCount);

			// children that are primitives get put in a mesh
			GameObject[] primitives, nonPrimitives;
			FilterPrimitives(nodeTransform, out primitives, out nonPrimitives);
			if (primitives.Length > 0)
			{
				node.Mesh = ExportMesh(nodeTransform.name, primitives);

				// associate unity meshes with gltf mesh id
				foreach (var prim in primitives)
				{
					_primOwner[new PrimKey { Mesh = getMesh(prim), Material = getMaterial(prim) }] = node.Mesh;
				}
			}

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				node.Children = new List<NodeId>(nonPrimitives.Length);
				foreach(var child in nonPrimitives)
				{
					node.Children.Add(ExportNode(child.transform, nodeCount));
				}
			}

			return id;
		}

		private void FilterPrimitives(Transform transform, out GameObject[] primitives, out GameObject[] nonPrimitives)
		{
			var childCount = transform.childCount;
			var prims = new List<GameObject>(childCount+1);
			var nonPrims = new List<GameObject>(childCount);

			// add another primitive if the root object also has a mesh
			if (GLTFUtils.isValidMeshObject(transform.gameObject))
			{
				prims.Add(transform.gameObject);
			}

			for (var i = 0; i < childCount; i++)
			{
				var go = transform.GetChild(i).gameObject;
					nonPrims.Add(go);
			}

			primitives = prims.ToArray();
			nonPrimitives = nonPrims.ToArray();
		}

		private static bool IsPrimitive(GameObject gameObject)
		{
			/*
			 * Primitives have the following properties:
			 * - have no children
			 * - have no non-default local transform properties
			 * - have MeshFilter and MeshRenderer components
			 */
			return gameObject.transform.childCount == 0
				&& gameObject.transform.localPosition == Vector3.zero
				&& gameObject.transform.localRotation == Quaternion.identity
				&& gameObject.transform.localScale == Vector3.one
				&& GLTFUtils.isValidMeshObject(gameObject);
		}

		private MeshId ExportMesh(string name, GameObject[] primitives)
		{
			// check if this set of primitives is already a mesh
			MeshId existingMeshId = null;
			var key = new PrimKey();
			foreach (var prim in primitives)
			{
				key.Mesh = getMesh(prim);
				key.Material = getMaterial(prim);

				MeshId tempMeshId;
				if (_primOwner.TryGetValue(key, out tempMeshId) && (existingMeshId == null || tempMeshId == existingMeshId))
				{
					existingMeshId = tempMeshId;
				}
				else
				{
					existingMeshId = null;
					break;
				}
			}

			// if so, return that mesh id
			if(existingMeshId != null)
				return existingMeshId;

			// if not, create new mesh and return its id
			var mesh = new GLTF.Schema.Mesh();

			if (ExportNames)
			{
				mesh.Name = name;
			}

			mesh.Primitives = new List<MeshPrimitive>(primitives.Length);
			foreach (var prim in primitives)
			{
				mesh.Primitives.AddRange(ExportPrimitive(prim));
			}

			var id = new MeshId
			{
				Id = _root.Meshes.Count,
				Root = _root
			};
			_root.Meshes.Add(mesh);

			return id;
		}

		// a mesh *might* decode to multiple prims if there are submeshes
		private MeshPrimitive[] ExportPrimitive(GameObject gameObject)
		{
			var meshObj = getMesh(gameObject);

			var materialsObj = getMaterials(gameObject);

			var prims = new MeshPrimitive[meshObj.subMeshCount];

			// don't export any more accessors if this mesh is already exported
			MeshPrimitive[] primVariations;
			if (_meshToPrims.TryGetValue(meshObj, out primVariations)
				&& meshObj.subMeshCount == primVariations.Length)
			{
				for (var i = 0; i < primVariations.Length; i++)
				{
					prims[i] = primVariations[i].Clone();
					prims[i].Material = ExportMaterial(materialsObj[i]);
				}

				return prims;
			}

			AccessorId aPosition = null, aNormal = null, aTangent = null,
				aTexcoord0 = null, aTexcoord1 = null, aColor0 = null;

			aPosition = ExportAccessor(meshObj.vertices, true);

			if (meshObj.normals.Length != 0)
				aNormal = ExportAccessor(meshObj.normals, true);

			if (meshObj.tangents.Length != 0)
				aTangent = ExportAccessor(meshObj.tangents, true);

			if (meshObj.uv.Length != 0)
				aTexcoord0 = ExportAccessor(meshObj.uv);

			if (meshObj.uv2.Length != 0)
				aTexcoord1 = ExportAccessor(meshObj.uv2);

			if (meshObj.colors.Length != 0)
				aColor0 = ExportAccessor(meshObj.colors);

			MaterialId lastMaterialId = null;

			for (var submesh = 0; submesh < meshObj.subMeshCount; submesh++)
			{
				var primitive = new MeshPrimitive();

				var triangles = meshObj.GetTriangles(submesh);
				primitive.Indices = ExportAccessor(FlipFaces(triangles), true);

				primitive.Attributes = new Dictionary<string, AccessorId>();
				primitive.Attributes.Add(SemanticProperties.POSITION, aPosition);

				if (aNormal != null)
					primitive.Attributes.Add(SemanticProperties.NORMAL, aNormal);
				if (aTangent != null)
					primitive.Attributes.Add(SemanticProperties.TANGENT, aTangent);
				if (aTexcoord0 != null)
					primitive.Attributes.Add(SemanticProperties.TexCoord(0), aTexcoord0);
				if (aTexcoord1 != null)
					primitive.Attributes.Add(SemanticProperties.TexCoord(1), aTexcoord1);
				if (aColor0 != null)
					primitive.Attributes.Add(SemanticProperties.Color(0), aColor0);

				if (submesh < materialsObj.Length)
				{
					primitive.Material = ExportMaterial(materialsObj[submesh]);
					lastMaterialId = primitive.Material;
				}
				else
				{
					primitive.Material = lastMaterialId;
				}

				prims[submesh] = primitive;
			}

			_meshToPrims[meshObj] = prims;

			return prims;
		}

		private MaterialId ExportMaterial(UnityEngine.Material materialObj)
		{
			MaterialId id = GetMaterialId(_root, materialObj);
			if (id != null)
			{
				return id;
			}

			var material = new GLTF.Schema.Material();

			if (ExportNames)
			{
				material.Name = materialObj.name;
			}

			if (materialObj.HasProperty("_Cutoff"))
			{
				material.AlphaCutoff = materialObj.GetFloat("_Cutoff");
			}

			switch (materialObj.GetTag("RenderType", false, ""))
			{
				case "TransparentCutout":
					material.AlphaMode = AlphaMode.MASK;
					break;
				case "Transparent":
					material.AlphaMode = AlphaMode.BLEND;
					break;
				default:
					material.AlphaMode = AlphaMode.OPAQUE;
					break;
			}

			material.DoubleSided = materialObj.HasProperty("_Cull") &&
				materialObj.GetInt("_Cull") == (float)UnityEngine.Rendering.CullMode.Off;

			if (materialObj.HasProperty("_EmissionColor"))
			{
				material.EmissiveFactor = materialObj.GetColor("_EmissionColor").ToNumericsColor();
			}

			if (materialObj.HasProperty("_EmissionMap"))
			{
				var emissionTex = materialObj.GetTexture("_EmissionMap");

				if (emissionTex != null)
				{
					material.EmissiveTexture = ExportTextureInfo(emissionTex);

					ExportTextureTransform(material.EmissiveTexture, materialObj, "_EmissionMap");

				}
			}

			if (materialObj.HasProperty("_BumpMap"))
			{
				var normalTex = materialObj.GetTexture("_BumpMap");

				if (normalTex != null)
				{
					material.NormalTexture = ExportNormalTextureInfo(normalTex, materialObj);
					ExportTextureTransform(material.NormalTexture, materialObj, "_BumpMap");
				}
			}

			switch (materialObj.shader.name)
			{
				case "Standard":
				case "GLTF/GLTFStandard":
					material.PbrMetallicRoughness = ExportPBRMetallicRoughness(materialObj);
					if (materialObj.HasProperty("_OcclusionMap"))
					{
						var occTex = materialObj.GetTexture("_OcclusionMap");
						if (occTex != null)
						{
							// Pack occlusion with metallicRoughness if any
							if (material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
							{
								var info = new OcclusionTextureInfo();
								if (materialObj.HasProperty("_OcclusionStrength"))
								{
									info.Strength = materialObj.GetFloat("_OcclusionStrength");
								}
								info.Index = material.PbrMetallicRoughness.MetallicRoughnessTexture.Index;
								material.OcclusionTexture = info;
							}
							else
							{
								material.OcclusionTexture = ExportOcclusionTextureInfo(occTex, materialObj);
							}
						}
					}

					break;
				case "Standard (Specular setup)":
					KHR_materials_pbrSpecularGlossinessExtension pbr = convertSpecular(materialObj);
					material.Extensions.Add(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME, pbr);
					registerExtension(KHR_materials_pbrSpecularGlossinessExtensionFactory.EXTENSION_NAME);

					if (materialObj.HasProperty("_OcclusionMap"))
					{
						var occTex = materialObj.GetTexture("_OcclusionMap");
						if (occTex != null)
						{
							material.OcclusionTexture = ExportOcclusionTextureInfo(occTex, materialObj);
							ExportTextureTransform(material.OcclusionTexture, materialObj, "_OcclusionMap");
						}
					}

					break;
				case "GLTF/GLTFConstant":
					material.CommonConstant = ExportCommonConstant(materialObj);
					break;
			}

			_materials.Add(materialObj);

			id = new MaterialId {
				Id = _root.Materials.Count,
				Root = _root
			};
			_root.Materials.Add(material);

			return id;
		}

		private void ExportTextureTransform(TextureInfo def, UnityEngine.Material mat, string texName)
		{
			Vector2 offset = mat.GetTextureOffset(texName);
			Vector2 scale = mat.GetTextureScale(texName);

			if (offset == Vector2.zero && scale == Vector2.one) return;

			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(
					new string[] { ExtTextureTransformExtensionFactory.EXTENSION_NAME }
				);
			}
			else if (!_root.ExtensionsUsed.Contains(ExtTextureTransformExtensionFactory.EXTENSION_NAME))
			{
				_root.ExtensionsUsed.Add(ExtTextureTransformExtensionFactory.EXTENSION_NAME);
			}

			if (def.Extensions == null)
				def.Extensions = new Dictionary<string, Extension>();

			def.Extensions[ExtTextureTransformExtensionFactory.EXTENSION_NAME] = new ExtTextureTransformExtension(
				new GLTF.Math.Vector2(offset.x, -offset.y),
				new GLTF.Math.Vector2(scale.x, scale.y),
				0 // TODO: support UV channels
			);
		}

		private void registerExtension(string extension, bool isRequired=false)
		{
			if(!_root.ExtensionsUsed.Contains(extension))
			{
				_root.ExtensionsUsed.Add(extension);
			}

			if(isRequired && !_root.ExtensionsRequired.Contains(extension))
			{
				_root.ExtensionsRequired.Add(extension);
			}
		}

		private KHR_materials_pbrSpecularGlossinessExtension convertSpecular(UnityEngine.Material mat)
		{
			GLTF.Math.Color diffuseFactor = mat.GetColor("_Color").ToNumericsColor();
			TextureInfo diffuseTexture = mat.GetTexture("_MainTex") != null ? ExportTextureInfo(mat.GetTexture("_MainTex")) : null;

			TextureInfo specularGlossinessTexture = null;
			GLTF.Math.Color specularColor = Color.white.ToNumericsColor();
			float glossinessFactor = 1.0f;
			if (mat.GetTexture("_SpecGlossMap"))
			{
				specularGlossinessTexture = ExportTextureInfo(mat.GetTexture("_SpecGlossMap"));
				if(mat.HasProperty("_GlossMapScale"))
					glossinessFactor = mat.GetFloat("_GlossMapScale");
			}
			else
			{
				specularColor = mat.GetColor("_SpecColor").ToNumericsColor();
				if (mat.HasProperty("_Glossiness"))
					glossinessFactor = mat.GetFloat("_Glossiness");
			}

			GLTF.Math.Vector3 specularFactor = new GLTF.Math.Vector3(specularColor.R, specularColor.G, specularColor.B);


			return new KHR_materials_pbrSpecularGlossinessExtension(diffuseFactor, diffuseTexture, specularFactor, glossinessFactor, specularGlossinessTexture);
		}

		private NormalTextureInfo ExportNormalTextureInfo(UnityEngine.Texture texture, UnityEngine.Material material)
		{
			var info = new NormalTextureInfo();
			info.Index = ExportTexture(_textureCache.handleNormalMap((Texture2D)texture));
			if (material.HasProperty("_BumpScale"))
			{
				info.Scale = material.GetFloat("_BumpScale");
			}

			return info;
		}

		private OcclusionTextureInfo ExportOcclusionTextureInfo(UnityEngine.Texture texture, UnityEngine.Material material)
		{
			var info = new OcclusionTextureInfo();

			info.Index = ExportTexture(texture);

			if (material.HasProperty("_OcclusionStrength"))
			{
				info.Strength = material.GetFloat("_OcclusionStrength");
			}

			return info;
		}

		private PbrMetallicRoughness ExportPBRMetallicRoughness(UnityEngine.Material material)
		{
			var pbr = new PbrMetallicRoughness();

			if (material.HasProperty("_Color"))
			{
				pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColor();
			}

			if (material.HasProperty("_MainTex"))
			{
				var mainTex = material.GetTexture("_MainTex");

				if (mainTex != null)
				{
					pbr.BaseColorTexture = ExportTextureInfo(mainTex);
					ExportTextureTransform(pbr.BaseColorTexture, material, "_MainTex");
				}
			}

			if (material.HasProperty("_Metallic"))
			{
				pbr.MetallicFactor = material.GetFloat("_Metallic");
			}

			if (material.HasProperty("_Roughness"))
			{
				pbr.RoughnessFactor = material.GetFloat("_Roughness");
			}
			else if (material.HasProperty("_Glossiness"))
			{
				pbr.RoughnessFactor = 1 - material.GetFloat("_Glossiness");
			}

			if (material.HasProperty("_MetallicRoughnessMap"))
			{
				var mrTex = material.GetTexture("_MetallicRoughnessMap");

				if (mrTex != null)
				{
					pbr.MetallicRoughnessTexture = ExportTextureInfo(mrTex);
					ExportTextureTransform(pbr.MetallicRoughnessTexture, material, "_MetallicRoughnessMap");
				}
			}
			else if (material.HasProperty("_MetallicGlossMap"))
			{
				var mgTex = material.GetTexture("_MetallicGlossMap") as Texture2D;

				if (mgTex != null)
				{
					var occTex = (material.HasProperty("_OcclusionMap") ? material.GetTexture("_OcclusionMap") as Texture2D : null);
					pbr.MetallicRoughnessTexture = ExportTextureInfo(_textureCache.packOcclusionMetalRough(mgTex, occTex));
				}
			}

			return pbr;
		}

		private MaterialCommonConstant ExportCommonConstant(UnityEngine.Material materialObj)
		{
			if (_root.ExtensionsUsed == null)
			{
				_root.ExtensionsUsed = new List<string>(new string[] { "KHR_materials_common" });
			}
			else if(!_root.ExtensionsUsed.Contains("KHR_materials_common"))
				_root.ExtensionsUsed.Add("KHR_materials_common");

			var constant = new MaterialCommonConstant();

			if (materialObj.HasProperty("_AmbientFactor"))
			{
				constant.AmbientFactor = materialObj.GetColor("_AmbientFactor").ToNumericsColor();
			}

			if (materialObj.HasProperty("_LightMap"))
			{
				var lmTex = materialObj.GetTexture("_LightMap");

				if (lmTex != null)
				{
					constant.LightmapTexture = ExportTextureInfo(lmTex);
					ExportTextureTransform(constant.LightmapTexture, materialObj, "_LightMap");
				}

			}

			if (materialObj.HasProperty("_LightFactor"))
			{
				constant.LightmapFactor = materialObj.GetColor("_LightFactor").ToNumericsColor();
			}

			return constant;
		}

		private TextureInfo ExportTextureInfo(UnityEngine.Texture texture)
		{
			var info = new TextureInfo();

			info.Index = ExportTexture(texture);

			return info;
		}

		private TextureId ExportTexture(UnityEngine.Texture textureObj)
		{
			TextureId id = GetTextureId(_root, textureObj);
			if (id != null)
			{
				return id;
			}

			var texture = new GLTF.Schema.Texture();

			//If texture name not set give it a unique name using count
			if (textureObj.name == "")
			{
				textureObj.name = (_root.Textures.Count + 1).ToString();
			}

			if (ExportNames)
			{
				texture.Name = textureObj.name;
			}

			texture.Source = ExportImage(textureObj);
			texture.Sampler = ExportSampler(textureObj);

			_textures.Add(textureObj);

			id = new TextureId {
				Id = _root.Textures.Count,
				Root = _root
			};

			_root.Textures.Add(texture);

			return id;
		}

		private ImageId ExportImage(UnityEngine.Texture texture)
		{
			string imagePath = GLTFUtils.buildImageName((Texture2D)texture);
			ImageId id = GetImageId(_root, texture);
			if(id != null)
			{
				return id;
			}

			var image = new Image();

			if (ExportNames)
			{
				image.Name = texture.name;
			}

			_images.Add(texture as Texture2D);

			image.Uri = imagePath;

			id = new ImageId {
				Id = _root.Images.Count,
				Root = _root
			};

			_root.Images.Add(image);

			return id;
		}

		private SamplerId ExportSampler(UnityEngine.Texture texture)
		{
			var samplerId = GetSamplerId(_root, texture);
			if (samplerId != null)
				return samplerId;

			var sampler = new Sampler();

			if (texture.wrapMode == TextureWrapMode.Clamp)
			{
				sampler.WrapS = GLTF.Schema.WrapMode.ClampToEdge;
				sampler.WrapT = GLTF.Schema.WrapMode.ClampToEdge;
			}
			else
			{
				sampler.WrapS = GLTF.Schema.WrapMode.Repeat;
				sampler.WrapT = GLTF.Schema.WrapMode.Repeat;
			}

			if(texture.filterMode == FilterMode.Point)
			{
				sampler.MinFilter = MinFilterMode.NearestMipmapNearest;
				sampler.MagFilter = MagFilterMode.Nearest;
			}
			else if(texture.filterMode == FilterMode.Bilinear)
			{
				sampler.MinFilter = MinFilterMode.Linear;
				sampler.MagFilter = MagFilterMode.Linear;
			}
			else
			{
				sampler.MinFilter = MinFilterMode.LinearMipmapLinear;
				sampler.MagFilter = MagFilterMode.Linear;
			}

			samplerId = new SamplerId
			{
				Id = _root.Samplers.Count,
				Root = _root
			};

			_root.Samplers.Add(sampler);

			return samplerId;
		}

		private Vector2[] FlipY(Vector2[] arr)
		{
			var len = arr.Length;
			for(var i = 0; i < len; i++)
			{
				arr[i].y = 1 - arr[i].y;
			}
			return arr;
		}

		private Vector3[] InvertZ(Vector3[] arr)
		{
			var len = arr.Length;
			for(var i = 0; i < len; i++)
			{
				arr[i].z = -arr[i].z;
			}
			return arr;
		}

		private Vector4[] InvertW(Vector4[] arr)
		{
			var len = arr.Length;
			for(var i = 0; i < len; i++)
			{
				arr[i].w = -arr[i].w;
			}
			return arr;
		}

		private int[] FlipFaces(int[] arr)
		{
			var triangles = new int[arr.Length];
			for (int i = 0; i < arr.Length; i += 3)
			{
				triangles[i + 2] = arr[i];
				triangles[i + 1] = arr[i + 1];
				triangles[i] = arr[i + 2];
			}
			return triangles;
		}

		private AccessorId ExportAccessor(int[] arr, bool isIndices = false)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			int min = arr[0];
			int max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			var byteOffset = _bufferWriter.BaseStream.Position;
			if (_forceIndicesUint)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;
				foreach (var v in arr)
				{
					_bufferWriter.Write((uint)v);

				}
			}
			else if (max <= byte.MaxValue && min >= byte.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedByte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((byte)v);
				}
			}
			else if (max <= sbyte.MaxValue && min >= sbyte.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Byte;

				foreach (var v in arr)
				{
					_bufferWriter.Write((sbyte)v);
				}
			}
			else if (max <= short.MaxValue && min >= short.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Short;

				foreach (var v in arr)
				{
					_bufferWriter.Write((short)v);
				}
			}
			else if (max <= ushort.MaxValue && min >= ushort.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedShort;

				foreach (var v in arr)
				{
					_bufferWriter.Write((ushort)v);
				}
			}
			else if (min >= uint.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;

				foreach (var v in arr)
				{
					_bufferWriter.Write((uint)v);
				}
			}
			else
			{
				accessor.ComponentType = GLTFComponentType.Float;

				foreach (var v in arr)
				{
					_bufferWriter.Write((float)v);
				}
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId {
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(float[] arr)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.SCALAR;

			float min = arr[0];
			float max = arr[0];

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur < min)
				{
					min = cur;
				}
				if (cur > max)
				{
					max = cur;
				}
			}

			accessor.Min = new List<double> { min };
			accessor.Max = new List<double> { max };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var value in arr)
			{
				_bufferWriter.Write(value);
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};

			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector2[] arr)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC2;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float maxX = arr[0].x;
			float maxY = arr[0].y;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
			}

			accessor.Min = new List<double> { minX, minY };
			accessor.Max = new List<double> { maxX, maxY };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var vec in arr) {
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId {
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector3[] arr, bool switchHandedness=false)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC3;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ };
			accessor.Max = new List<double> { maxX, maxY, maxZ };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var vec in arr) {
				if(switchHandedness)
				{
					Vector3 vect = vec.switchHandedness();
					_bufferWriter.Write(vect.x);
					_bufferWriter.Write(vect.y);
					_bufferWriter.Write(vect.z);
				}
				else
				{
					_bufferWriter.Write(vec.x);
					_bufferWriter.Write(vec.y);
					_bufferWriter.Write(vec.z);
				}
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId {
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessorUint(Vector4[] arr)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.UnsignedShort;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var vec in arr)
			{
				_bufferWriter.Write((ushort)vec.x);
				_bufferWriter.Write((ushort)vec.y);
				_bufferWriter.Write((ushort)vec.z);
				_bufferWriter.Write((ushort)vec.w);
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Vector4[] arr, bool switchHandedness=false)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minX = arr[0].x;
			float minY = arr[0].y;
			float minZ = arr[0].z;
			float minW = arr[0].w;
			float maxX = arr[0].x;
			float maxY = arr[0].y;
			float maxZ = arr[0].z;
			float maxW = arr[0].w;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.x < minX)
				{
					minX = cur.x;
				}
				if (cur.y < minY)
				{
					minY = cur.y;
				}
				if (cur.z < minZ)
				{
					minZ = cur.z;
				}
				if (cur.w < minW)
				{
					minW = cur.w;
				}
				if (cur.x > maxX)
				{
					maxX = cur.x;
				}
				if (cur.y > maxY)
				{
					maxY = cur.y;
				}
				if (cur.z > maxZ)
				{
					maxZ = cur.z;
				}
				if (cur.w > maxW)
				{
					maxW = cur.w;
				}
			}

			accessor.Min = new List<double> { minX, minY, minZ, minW };
			accessor.Max = new List<double> { maxX, maxY, maxZ, maxW };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var vec in arr) {
				Vector4 vect = switchHandedness ? vec.switchHandedness() : vec;
				_bufferWriter.Write(vect.x);
				_bufferWriter.Write(vect.y);
				_bufferWriter.Write(vect.z);
				_bufferWriter.Write(vect.w);
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId {
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(UnityEngine.Color[] arr)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.VEC4;

			float minR = arr[0].r;
			float minG = arr[0].g;
			float minB = arr[0].b;
			float minA = arr[0].a;
			float maxR = arr[0].r;
			float maxG = arr[0].g;
			float maxB = arr[0].b;
			float maxA = arr[0].a;

			for (var i = 1; i < count; i++)
			{
				var cur = arr[i];

				if (cur.r < minR)
				{
					minR = cur.r;
				}
				if (cur.g < minG)
				{
					minG = cur.g;
				}
				if (cur.b < minB)
				{
					minB = cur.b;
				}
				if (cur.a < minA)
				{
					minA = cur.a;
				}
				if (cur.r > maxR)
				{
					maxR = cur.r;
				}
				if (cur.g > maxG)
				{
					maxG = cur.g;
				}
				if (cur.b > maxB)
				{
					maxB = cur.b;
				}
				if (cur.a > maxA)
				{
					maxA = cur.a;
				}
			}

			accessor.Min = new List<double> { minR, minG, minB, minA };
			accessor.Max = new List<double> { maxR, maxG, maxB, maxA };

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var color in arr) {
				_bufferWriter.Write(color.r);
				_bufferWriter.Write(color.g);
				_bufferWriter.Write(color.b);
				_bufferWriter.Write(color.a);
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId {
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private AccessorId ExportAccessor(Matrix4x4[] arr, bool switchHandedness = false)
		{
			var count = arr.Length;

			if (count == 0)
			{
				throw new Exception("Accessors can not have a count of 0.");
			}

			var accessor = new Accessor();
			accessor.ComponentType = GLTFComponentType.Float;
			accessor.Count = count;
			accessor.Type = GLTFAccessorAttributeType.MAT4;

			// Dont serialize min/max for matrices

			var byteOffset = _bufferWriter.BaseStream.Position;

			foreach (var mat in arr)
			{
				Matrix4x4 mamat = switchHandedness ? mat.switchHandedness() : mat;
				for (int i = 0; i < 4; ++i)
				{
					Vector4 col = mamat.GetColumn(i);
					_bufferWriter.Write(col.x);
					_bufferWriter.Write(col.y);
					_bufferWriter.Write(col.z);
					_bufferWriter.Write(col.w);
				}
			}

			var byteLength = _bufferWriter.BaseStream.Position - byteOffset;

			accessor.BufferView = ExportBufferView((int)byteOffset, (int)byteLength);

			var id = new AccessorId
			{
				Id = _root.Accessors.Count,
				Root = _root
			};
			_root.Accessors.Add(accessor);

			return id;
		}

		private BufferViewId ExportBufferView(int byteOffset, int byteLength)
		{
			var bufferView = new BufferView {
				Buffer = _bufferId,
				ByteOffset = byteOffset,
				ByteLength = byteLength,
			};

			var id = new BufferViewId {
				Id = _root.BufferViews.Count,
				Root = _root
			};

			_root.BufferViews.Add(bufferView);

			return id;
		}

		public MaterialId GetMaterialId(GLTFRoot root, UnityEngine.Material materialObj)
		{
			for (var i = 0; i < _materials.Count; i++)
			{
				if (_materials[i] == materialObj)
				{
					return new MaterialId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public TextureId GetTextureId(GLTFRoot root, UnityEngine.Texture textureObj)
		{
			for (var i = 0; i < _textures.Count; i++)
			{
				if (_textures[i] == textureObj)
				{
					return new TextureId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public ImageId GetImageId(GLTFRoot root, UnityEngine.Texture imageObj)
		{
			for (var i = 0; i < _images.Count; i++)
			{
				if (_images[i] == imageObj)
				{
					return new ImageId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public SamplerId GetSamplerId(GLTFRoot root, UnityEngine.Texture textureObj)
		{
			for (var i = 0; i < root.Samplers.Count; i++)
			{
				bool filterIsNearest = root.Samplers[i].MinFilter == MinFilterMode.Nearest
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapNearest
					|| root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapNearest;

				bool filterIsLinear = root.Samplers[i].MinFilter == MinFilterMode.Linear
					|| root.Samplers[i].MinFilter == MinFilterMode.NearestMipmapLinear;

				bool filterMatched = textureObj.filterMode == FilterMode.Point && filterIsNearest
					|| textureObj.filterMode == FilterMode.Bilinear && filterIsLinear
					|| textureObj.filterMode == FilterMode.Trilinear && root.Samplers[i].MinFilter == MinFilterMode.LinearMipmapLinear;

				bool wrapMatched = textureObj.wrapMode == TextureWrapMode.Clamp && root.Samplers[i].WrapS == GLTF.Schema.WrapMode.ClampToEdge
					|| textureObj.wrapMode == TextureWrapMode.Repeat && root.Samplers[i].WrapS != GLTF.Schema.WrapMode.ClampToEdge;

				if (filterMatched && wrapMatched)
				{
					return new SamplerId
					{
						Id = i,
						Root = root
					};
				}
			}

			return null;
		}

		public enum ROTATION_TYPE
		{
			UNKNOWN,
			QUATERNION,
			EULER
		};

		private struct TargetCurveSet
		{
			public AnimationCurve[] translationCurves;
			public AnimationCurve[] rotationCurves;
			//Additional curve types
			public AnimationCurve[] localEulerAnglesRaw;
			public AnimationCurve[] m_LocalEuler;
			public AnimationCurve[] scaleCurves;
			public ROTATION_TYPE rotationType;
			public void Init()
			{
				translationCurves = new AnimationCurve[3];
				rotationCurves = new AnimationCurve[4];
				scaleCurves = new AnimationCurve[3];
			}
		}

		static int bakingFramerate = 30; // FPS
		static bool bake = true;

		// Parses Animation/Animator component and generate a glTF animation for the active clip
		public void exportAnimationFromNode(ref Transform transform, ref GLTF.Schema.Animation anim)
		{
			Animator a = transform.GetComponent<Animator>();
			if (a != null)
			{
				AnimationClip[] clips = AnimationUtility.GetAnimationClips(transform.gameObject);
				for (int i = 0; i < clips.Length; i++)
				{
					//FIXME It seems not good to generate one animation per animator.
					convertClipToGLTFAnimation(ref clips[i], ref transform, ref anim);
				}
			}

			UnityEngine.Animation animation = transform.GetComponent<UnityEngine.Animation>();
			if (animation != null)
			{
				AnimationClip[] clips = AnimationUtility.GetAnimationClips(transform.gameObject);
				for (int i = 0; i < clips.Length; i++)
				{
					//FIXME It seems not good to generate one animation per animator.
					convertClipToGLTFAnimation(ref clips[i], ref transform, ref anim);
				}
			}
		}

		private int getTargetIdFromTransform(ref Transform transform)
		{
			if (_exportedTransforms.ContainsKey(transform.GetInstanceID()))
			{
				return _exportedTransforms[transform.GetInstanceID()];
			}
			else
			{
				Debug.Log(transform.name + " " + transform.GetInstanceID());
				return 0;
			}
		}

		private AccessorId ExportAccessor()
		{
			var id = new AccessorId
			{
				Id = 5,
				Root = _root
			};

			return id;
		}

		private void convertClipToGLTFAnimation(ref AnimationClip clip, ref Transform transform, ref GLTF.Schema.Animation animation)
		{
			// Generate GLTF.Schema.AnimationChannel and GLTF.Schema.AnimationSampler
			// 1 channel per node T/R/S, one sampler per node T/R/S
			// Need to keep a list of nodes to convert to indexes

			// 1. browse clip, collect all curves and create a TargetCurveSet for each target
			Dictionary<string, TargetCurveSet> targetCurvesBinding = new Dictionary<string, TargetCurveSet>();
			collectClipCurves(clip, ref targetCurvesBinding);

			// Baking needs all properties, fill missing curves with transform data in 2 keyframes (start, endTime)
			// where endTime is clip duration
			// Note: we should avoid creating curves for a property if none of it's components is animated
			generateMissingCurves(clip.length, ref transform, ref targetCurvesBinding);

			if (bake)
			{
				// Bake animation for all animated nodes
				foreach (string target in targetCurvesBinding.Keys)
				{
					Transform targetTr = target.Length > 0 ? transform.Find(target) : transform;
					if (targetTr == null || targetTr.GetComponent<SkinnedMeshRenderer>())
					{
						continue;
					}


					// Initialize data
					// Bake and populate animation data
					float[] times = null;
					Vector3[] positions = null;
					Vector3[] scales = null;
					Vector4[] rotations = null;
					bakeCurveSet(targetCurvesBinding[target], clip.length, bakingFramerate, ref times, ref positions, ref rotations, ref scales);

					int channelTargetId = getTargetIdFromTransform(ref targetTr);
					AccessorId timeAccessor = ExportAccessor(times);

					// Create channel
					AnimationChannel Tchannel = new AnimationChannel();
					AnimationChannelTarget TchannelTarget = new AnimationChannelTarget();
					TchannelTarget.Path = GLTFAnimationChannelPath.translation;
					TchannelTarget.Node = new NodeId
					{
						Id = channelTargetId,
						Root = _root
					};

					Tchannel.Target = TchannelTarget;

					AnimationSampler Tsampler = new AnimationSampler();
					Tsampler.Input = timeAccessor;
					Tsampler.Output = ExportAccessor(positions, true); // Vec3 for translation
					Tchannel.Sampler = new AnimationSamplerId
					{
						Id = animation.Samplers.Count,
						GLTFAnimation = animation,
						Root = _root
					};

					animation.Samplers.Add(Tsampler);
					animation.Channels.Add(Tchannel);

					// Rotation
					AnimationChannel Rchannel = new AnimationChannel();
					AnimationChannelTarget RchannelTarget = new AnimationChannelTarget();
					RchannelTarget.Path = GLTFAnimationChannelPath.rotation;
					RchannelTarget.Node = new NodeId
					{
						Id = channelTargetId,
						Root = _root
					};

					Rchannel.Target = RchannelTarget;

					AnimationSampler Rsampler = new AnimationSampler();
					Rsampler.Input = timeAccessor; // Float, for time
					Rsampler.Output = ExportAccessor(rotations, true); // Vec4 for
					Rchannel.Sampler = new AnimationSamplerId
					{
						Id = animation.Samplers.Count,
						GLTFAnimation = animation,
						Root = _root
					};

					animation.Samplers.Add(Rsampler);
					animation.Channels.Add(Rchannel);

					// Scale
					AnimationChannel Schannel = new AnimationChannel();
					AnimationChannelTarget SchannelTarget = new AnimationChannelTarget();
					SchannelTarget.Path = GLTFAnimationChannelPath.scale;
					SchannelTarget.Node = new NodeId
					{
						Id = channelTargetId,
						Root = _root
					};

					Schannel.Target = SchannelTarget;

					AnimationSampler Ssampler = new AnimationSampler();
					Ssampler.Input = timeAccessor; // Float, for time
					Ssampler.Output = ExportAccessor(scales); // Vec3 for scale
					Schannel.Sampler = new AnimationSamplerId
					{
						Id = animation.Samplers.Count,
						GLTFAnimation = animation,
						Root = _root
					};

					animation.Samplers.Add(Ssampler);
					animation.Channels.Add(Schannel);
				}
			}
			else
			{
				Debug.LogError("Only baked animation is supported for now. Skipping animation");
			}

		}

		private void collectClipCurves(AnimationClip clip, ref Dictionary<string, TargetCurveSet> targetCurves)
		{
			foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
			{
				AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

				if (!targetCurves.ContainsKey(binding.path))
				{
					TargetCurveSet curveSet = new TargetCurveSet();
					curveSet.Init();
					targetCurves.Add(binding.path, curveSet);
				}

				TargetCurveSet current = targetCurves[binding.path];
				if (binding.propertyName.Contains("m_LocalPosition"))
				{
					if (binding.propertyName.Contains(".x"))
						current.translationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.translationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.translationCurves[2] = curve;
				}
				else if (binding.propertyName.Contains("m_LocalScale"))
				{
					if (binding.propertyName.Contains(".x"))
						current.scaleCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.scaleCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.scaleCurves[2] = curve;
				}
				else if (binding.propertyName.ToLower().Contains("localrotation"))
				{
					current.rotationType = ROTATION_TYPE.QUATERNION;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
					else if (binding.propertyName.Contains(".w"))
						current.rotationCurves[3] = curve;
				}
				// Takes into account 'localEuler', 'localEulerAnglesBaked' and 'localEulerAnglesRaw'
				else if (binding.propertyName.ToLower().Contains("localeuler"))
				{
					current.rotationType = ROTATION_TYPE.EULER;
					if (binding.propertyName.Contains(".x"))
						current.rotationCurves[0] = curve;
					else if (binding.propertyName.Contains(".y"))
						current.rotationCurves[1] = curve;
					else if (binding.propertyName.Contains(".z"))
						current.rotationCurves[2] = curve;
				}
				targetCurves[binding.path] = current;
			}
		}

		private void generateMissingCurves(float endTime, ref Transform tr, ref Dictionary<string, TargetCurveSet> targetCurvesBinding)
		{
			foreach (string target in targetCurvesBinding.Keys)
			{
				Transform targetTr = target.Length > 0 ? tr.Find(target) : tr;
				if (targetTr == null)
					continue;

				TargetCurveSet current = targetCurvesBinding[target];
				if (current.translationCurves[0] == null)
				{
					current.translationCurves[0] = createConstantCurve(targetTr.localPosition.x, endTime);
					current.translationCurves[1] = createConstantCurve(targetTr.localPosition.y, endTime);
					current.translationCurves[2] = createConstantCurve(targetTr.localPosition.z, endTime);
				}

				if (current.scaleCurves[0] == null)
				{
					current.scaleCurves[0] = createConstantCurve(targetTr.localScale.x, endTime);
					current.scaleCurves[1] = createConstantCurve(targetTr.localScale.y, endTime);
					current.scaleCurves[2] = createConstantCurve(targetTr.localScale.z, endTime);
				}

				if (current.rotationCurves[0] == null)
				{
					current.rotationCurves[0] = createConstantCurve(targetTr.localRotation.x, endTime);
					current.rotationCurves[1] = createConstantCurve(targetTr.localRotation.y, endTime);
					current.rotationCurves[2] = createConstantCurve(targetTr.localRotation.z, endTime);
					current.rotationCurves[3] = createConstantCurve(targetTr.localRotation.w, endTime);
				}
			}
		}

		private AnimationCurve createConstantCurve(float value, float endTime)
		{
			// No translation curves, adding them
			AnimationCurve curve = new AnimationCurve();
			curve.AddKey(0, value);
			curve.AddKey(endTime, value);
			return curve;
		}

		private void bakeCurveSet(TargetCurveSet curveSet, float length, int bakingFramerate, ref float[] times, ref Vector3[] positions, ref Vector4[] rotations, ref Vector3[] scales)
		{
			int nbSamples = (int)(length * 30);
			float deltaTime = length / nbSamples;

			// Initialize Arrays
			times = new float[nbSamples];
			positions = new Vector3[nbSamples];
			scales = new Vector3[nbSamples];
			rotations = new Vector4[nbSamples];

			// Assuming all the curves exist now
			for (int i = 0; i < nbSamples; ++i)
			{
				float currentTime = i * deltaTime;
				times[i] = currentTime;
				positions[i] = new Vector3(curveSet.translationCurves[0].Evaluate(currentTime), curveSet.translationCurves[1].Evaluate(currentTime), curveSet.translationCurves[2].Evaluate(currentTime));
				scales[i] = new Vector3(curveSet.scaleCurves[0].Evaluate(currentTime), curveSet.scaleCurves[1].Evaluate(currentTime), curveSet.scaleCurves[2].Evaluate(currentTime));
				if (curveSet.rotationType == ROTATION_TYPE.EULER)
				{
					Quaternion eulerToQuat = Quaternion.Euler(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime));
					rotations[i] = new Vector4(eulerToQuat.x, eulerToQuat.y, eulerToQuat.z, eulerToQuat.w);
				}
				else
				{
					rotations[i] = new Vector4(curveSet.rotationCurves[0].Evaluate(currentTime), curveSet.rotationCurves[1].Evaluate(currentTime), curveSet.rotationCurves[2].Evaluate(currentTime), curveSet.rotationCurves[3].Evaluate(currentTime));
				}
			}
		}

		private void exportSkinFromNode(Transform transform)
		{
			PrimKey key = new PrimKey();
			UnityEngine.Mesh mesh = getMesh(transform.gameObject);
			key.Mesh = mesh;
			key.Material = getMaterial(transform.gameObject);
			MeshId val;
			if(!_primOwner.TryGetValue(key, out val))
			{
				Debug.Log("No mesh found for skin");
				return;
			}
			SkinnedMeshRenderer skin = transform.GetComponent<SkinnedMeshRenderer>();
			GLTF.Schema.Skin gltfSkin = new Skin();

			for (int i = 0; i < skin.bones.Length; ++i)
			{
				gltfSkin.Joints.Add(
					new NodeId
					{
						Id = _exportedTransforms[skin.bones[i].GetInstanceID()],
						Root = _root
					});
			}

			gltfSkin.InverseBindMatrices = ExportAccessor(mesh.bindposes, true);

			Vector4[] bones = boneWeightToBoneVec4(mesh.boneWeights);
			Vector4[] weights = boneWeightToWeightVec4(mesh.boneWeights);

			GLTF.Schema.Mesh gltfMesh = _root.Meshes[val.Id];
			foreach(MeshPrimitive prim in gltfMesh.Primitives)
			{
				if(!prim.Attributes.ContainsKey("JOINTS_0"))
					prim.Attributes.Add("JOINTS_0", ExportAccessorUint(bones));
				if (!prim.Attributes.ContainsKey("WEIGHTS_0"))
					prim.Attributes.Add("WEIGHTS_0", ExportAccessor(weights));
			}

			_root.Nodes[_exportedTransforms[transform.GetInstanceID()]].Skin = new SkinId() { Id = _root.Skins.Count, Root = _root };
			_root.Skins.Add(gltfSkin);
		}

		private Vector4[] boneWeightToBoneVec4(BoneWeight[] bw)
		{
			Vector4[] bones = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				bones[i] = new Vector4(bw[i].boneIndex0, bw[i].boneIndex1, bw[i].boneIndex2, bw[i].boneIndex3);
			}

			return bones;
		}

		private Vector4[] boneWeightToWeightVec4(BoneWeight[] bw)
		{
			Vector4[] weights = new Vector4[bw.Length];
			for (int i = 0; i < bw.Length; ++i)
			{
				weights[i] = new Vector4(bw[i].weight0, bw[i].weight1, bw[i].weight2, bw[i].weight3);
			}

			return weights;
		}
	}
}
