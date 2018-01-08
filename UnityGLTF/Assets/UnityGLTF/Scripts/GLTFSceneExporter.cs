using System;
using System.Collections.Generic;
using System.IO;
using GLTF.Schema;
using UnityEngine;
using UnityGLTF.Extensions;

namespace UnityGLTF
{
	public class GLTFSceneExporter
	{
		private Transform[] _rootTransforms;
		private GLTFRoot _root;
		private BufferId _bufferId;
		private GLTF.Schema.Buffer _buffer;
		private BinaryWriter _bufferWriter;
		private List<Texture2D> _images;
		private List<UnityEngine.Texture> _textures;
		private List<UnityEngine.Material> _materials;

		protected struct PrimKey
		{
			public UnityEngine.Mesh Mesh;
			public UnityEngine.Material Material;
		}
		private readonly Dictionary<PrimKey, MeshId> _primOwner = new Dictionary<PrimKey, MeshId>();
		private readonly Dictionary<UnityEngine.Mesh, MeshPrimitive[]> _meshToPrims = new Dictionary<UnityEngine.Mesh, MeshPrimitive[]>();

		public bool ExportNames = true;

		/// <summary>
		/// Create a GLTFExporter that exports out a transform
		/// </summary>
		/// <param name="rootTransforms">Root transform of object to export</param>
		public GLTFSceneExporter(Transform[] rootTransforms)
		{
			_rootTransforms = rootTransforms;
			_root = new GLTFRoot{
				Accessors = new List<Accessor>(),
				Asset = new Asset {
					Version = "2.0"
				},
				Buffers = new List<GLTF.Schema.Buffer>(),
				BufferViews = new List<BufferView>(),
				Images = new List<Image>(),
				Materials = new List<GLTF.Schema.Material>(),
				Meshes = new List<GLTF.Schema.Mesh>(),
				Nodes = new List<Node>(),
				Samplers = new List<Sampler>(),
				Scenes = new List<Scene>(),
				Textures = new List<GLTF.Schema.Texture>(),
			};

			_images = new List<Texture2D>();
			_materials = new List<UnityEngine.Material>();
			_textures = new List<UnityEngine.Texture>();

			_buffer = new GLTF.Schema.Buffer();
			_bufferId = new BufferId {
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

		/// <summary>
		/// Specifies the path and filename for the GLTF Json and binary
		/// </summary>
		/// <param name="path">File path for saving the GLTF and binary files</param>
		/// <param name="fileName">The name of the GLTF file</param>
		public void SaveGLTFandBin(string path, string fileName)
		{
			var binFile = File.Create(Path.Combine(path, fileName + ".bin"));
			_bufferWriter = new BinaryWriter(binFile);

			_root.Scene = ExportScene(fileName, _rootTransforms);

			_buffer.Uri = fileName + ".bin";
			_buffer.ByteLength = (int)_bufferWriter.BaseStream.Length;

			var gltfFile = File.CreateText(Path.Combine(path, fileName + ".gltf"));
			_root.Serialize(gltfFile);

#if WINDOWS_UWP
			gltfFile.Dispose();
			binFile.Dispose();
#else
			gltfFile.Close();
			binFile.Close();
#endif

			foreach (var image in _images)
			{
				Debug.Log(image.name);
				var renderTexture = RenderTexture.GetTemporary(image.width, image.height);
				Graphics.Blit(image, renderTexture);
				RenderTexture.active = renderTexture;
				var exportTexture = new Texture2D(image.width, image.height);
				exportTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
				exportTexture.Apply();
				File.WriteAllBytes(Path.Combine(path, image.name + ".png"), exportTexture.EncodeToPNG());
			}
		}

		private SceneId ExportScene(string name, Transform[] rootObjTransforms)
		{
			var scene = new Scene();

			if (ExportNames)
			{
				scene.Name = name;
			}

			scene.Nodes = new List<NodeId>(rootObjTransforms.Length);
			foreach (var transform in rootObjTransforms)
			{
				scene.Nodes.Add(ExportNode(transform));
			}

			_root.Scenes.Add(scene);

			return new SceneId {
				Id = _root.Scenes.Count - 1,
				Root = _root
			};
		}

		private NodeId ExportNode(Transform nodeTransform)
		{
			var node = new Node();

			if (ExportNames)
			{
				node.Name = nodeTransform.name;
			}

			node.SetUnityTransform(nodeTransform);

			var id = new NodeId {
				Id = _root.Nodes.Count,
				Root = _root
			};
			_root.Nodes.Add(node);

			// children that are primitives get put in a mesh
			GameObject[] primitives, nonPrimitives;
			FilterPrimitives(nodeTransform, out primitives, out nonPrimitives);
			if (primitives.Length > 0)
			{
				node.Mesh = ExportMesh(nodeTransform.name, primitives);

				// associate unity meshes with gltf mesh id
				foreach (var prim in primitives)
				{
					var filter = prim.GetComponent<MeshFilter>();
					var renderer = prim.GetComponent<MeshRenderer>();
					_primOwner[new PrimKey {Mesh = filter.sharedMesh, Material = renderer.sharedMaterial}] = node.Mesh;
				}
			}

			// children that are not primitives get added as child nodes
			if (nonPrimitives.Length > 0)
			{
				node.Children = new List<NodeId>(nonPrimitives.Length);
				foreach(var child in nonPrimitives)
				{
					node.Children.Add(ExportNode(child.transform));
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
			if (transform.gameObject.GetComponent<MeshFilter>() != null
				&& transform.gameObject.GetComponent<MeshRenderer>() != null)
			{
				prims.Add(transform.gameObject);
			}

			for (var i = 0; i < childCount; i++)
			{
				var go = transform.GetChild(i).gameObject;
				if (IsPrimitive(go))
					prims.Add(go);
				else
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
				&& gameObject.GetComponent<MeshFilter>() != null
				&& gameObject.GetComponent<MeshRenderer>() != null;
		}

		private MeshId ExportMesh(string name, GameObject[] primitives)
		{
			// check if this set of primitives is already a mesh
			MeshId existingMeshId = null;
			var key = new PrimKey();
			foreach (var prim in primitives)
			{
				var filter = prim.GetComponent<MeshFilter>();
				var renderer = prim.GetComponent<MeshRenderer>();
				key.Mesh = filter.sharedMesh;
				key.Material = renderer.sharedMaterial;

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
			var filter = gameObject.GetComponent<MeshFilter>();
			var meshObj = filter.sharedMesh;

			var renderer = gameObject.GetComponent<MeshRenderer>();
			var materialsObj = renderer.sharedMaterials;

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

			aPosition = ExportAccessor(InvertZ(meshObj.vertices));

			if (meshObj.normals.Length != 0)
				aNormal = ExportAccessor(InvertZ(meshObj.normals));

			if (meshObj.tangents.Length != 0)
				aTangent = ExportAccessor(InvertW(meshObj.tangents));

			if (meshObj.uv.Length != 0)
				aTexcoord0 = ExportAccessor(FlipY(meshObj.uv));

			if (meshObj.uv2.Length != 0)
				aTexcoord1 = ExportAccessor(FlipY(meshObj.uv2));

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

			if (materialObj.HasProperty("_OcclusionMap"))
			{
				var occTex = materialObj.GetTexture("_OcclusionMap");
				if (occTex != null)
				{
					material.OcclusionTexture = ExportOcclusionTextureInfo(occTex, materialObj);
					ExportTextureTransform(material.OcclusionTexture, materialObj, "_OcclusionMap");
				}
			}

			switch (materialObj.shader.name)
			{
				case "Standard":
				case "GLTF/GLTFStandard":
					material.PbrMetallicRoughness = ExportPBRMetallicRoughness(materialObj);
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

		private NormalTextureInfo ExportNormalTextureInfo(UnityEngine.Texture texture, UnityEngine.Material material)
		{
			var info = new NormalTextureInfo();

			info.Index = ExportTexture(texture);

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
				var mgTex = material.GetTexture("_MetallicGlossMap");

				if (mgTex != null)
				{
					pbr.MetallicRoughnessTexture = ExportTextureInfo(mgTex);
					ExportTextureTransform(pbr.MetallicRoughnessTexture, material, "_MetallicGlossMap");
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

			image.Uri = Uri.EscapeUriString(texture.name + ".png");

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
				sampler.MinFilter = MinFilterMode.NearestMipmapLinear;
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

			if (max <= byte.MaxValue && min >= byte.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedByte;

				foreach (var v in arr) {
					_bufferWriter.Write((byte)v);
				}
			}
			else if (max <= sbyte.MaxValue && min >= sbyte.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Byte;

				foreach (var v in arr) {
					_bufferWriter.Write((sbyte)v);
				}
			}
			else if (max <= short.MaxValue && min >= short.MinValue && !isIndices)
			{
				accessor.ComponentType = GLTFComponentType.Short;

				foreach (var v in arr) {
					_bufferWriter.Write((short)v);
				}
			}
			else if (max <= ushort.MaxValue && min >= ushort.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedShort;

				foreach (var v in arr) {
					_bufferWriter.Write((ushort)v);
				}
			}
			else if (min >= uint.MinValue)
			{
				accessor.ComponentType = GLTFComponentType.UnsignedInt;

				foreach (var v in arr) {
					_bufferWriter.Write((uint)v);
				}
			}
			else
			{
				accessor.ComponentType = GLTFComponentType.Float;

				foreach (var v in arr) {
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

		private AccessorId ExportAccessor(Vector3[] arr)
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
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
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

		private AccessorId ExportAccessor(Vector4[] arr)
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
				_bufferWriter.Write(vec.x);
				_bufferWriter.Write(vec.y);
				_bufferWriter.Write(vec.z);
				_bufferWriter.Write(vec.w);
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
	}
}
