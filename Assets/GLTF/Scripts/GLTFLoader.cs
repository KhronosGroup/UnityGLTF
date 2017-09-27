using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace GLTF
{
	public class GLTFLoader
	{
		public enum MaterialType
		{
			PbrMetallicRoughness,
			PbrSpecularGlossiness,
			CommonConstant,
			CommonPhong,
			CommonBlinn,
			CommonLambert
		}

		public bool Multithreaded = true;
		public int MaximumLod = 300;
		protected readonly string _gltfUrl;
		protected GLTFRoot _root;
		protected GameObject _lastLoadedScene;
		protected AsyncAction asyncAction;
		protected readonly Transform _sceneParent;

		protected readonly Material DefaultMaterial = new Material();
		protected readonly Dictionary<MaterialType, Shader> _shaderCache = new Dictionary<MaterialType, Shader>();
		protected readonly Dictionary<int, GameObject> _nodeMap = new Dictionary<int, GameObject>();

		public GLTFLoader(string gltfUrl, Transform parent = null)
		{
			_gltfUrl = gltfUrl;
			_sceneParent = parent;
			asyncAction = new AsyncAction();
		}

		public GameObject LastLoadedScene
		{
			get
			{
				return _lastLoadedScene;
			}
		}

		public virtual void SetShaderForMaterialType(MaterialType type, Shader shader)
		{
			_shaderCache.Add(type, shader);
		}

		public virtual IEnumerator Load(int sceneIndex = -1)
		{
			if (_root == null)
			{
				var www = UnityWebRequest.Get(_gltfUrl);

				yield return www.Send();
				if (www.responseCode >= 400 || www.responseCode == 0)
				{
					throw new WebRequestException(www);
				}

                var gltfData = www.downloadHandler.data;

				if (Multithreaded)
				{
					yield return asyncAction.RunOnWorkerThread(() => ParseGLTF(gltfData));
				}
				else
				{
					ParseGLTF(gltfData);
				}
			}

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

			if (_lastLoadedScene == null)
			{
				if (_root.Buffers != null)
				{
					foreach (var buffer in _root.Buffers)
					{
						yield return LoadBuffer(buffer);
					}
				}

				if (_root.Images != null)
				{
					foreach (var image in _root.Images)
					{
						yield return LoadImage(image);
					}
				}

				// generate these in advance instead of as-needed
				if (Multithreaded)
				{
					yield return asyncAction.RunOnWorkerThread(() => BuildMeshAttributes());
				}
			}

			var sceneObj = CreateScene(scene);

			if (_sceneParent != null)
			{
				sceneObj.transform.SetParent(_sceneParent, false);
			}

			_lastLoadedScene = sceneObj;
		}

		protected virtual void ParseGLTF(byte[] gltfData)
		{
			byte[] glbBuffer;
			_root = GLTFParser.ParseBinary(gltfData, out glbBuffer);

			if (glbBuffer != null)
			{
				_root.Buffers[0].Contents = glbBuffer;
			}
		}

		protected virtual void BuildMeshAttributes()
		{
			foreach (var mesh in _root.Meshes)
			{
				foreach (var primitive in mesh.Primitives)
				{
					primitive.BuildMeshAttributes();
				}
			}
		}

		protected virtual GameObject CreateScene(Scene scene)
		{
			var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node);
				nodeObj.transform.SetParent(sceneObj.transform, false);
			}

			foreach (var node in scene.Nodes)
			{
				CreateNodePrimitives(node.Value, _nodeMap[node.Id]);
			}

			foreach (var animation in _root.Animations)
			{
				AddAnimationToScene(scene, sceneObj, animation);
			}

			return sceneObj;
		}

		protected virtual GameObject CreateNode(NodeId nodeId)
		{
			var node = nodeId.Value;
			var nodeObj = new GameObject(node.Name ?? ("GLTFNode" + nodeId.Id));
			_nodeMap[nodeId.Id] = nodeObj;

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

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
					var childObj = CreateNode(child);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			return nodeObj;
		}

		/// <summary>
		/// Generate Animation components from glTF animations, and attach to game objects
		/// </summary>
		protected void AddAnimationToScene(Scene scene, GameObject sceneObj, Animation animation)
		{
			// create the animation clip that will contain animation data
			AnimationClip clip = new AnimationClip();
			clip.name = animation.Name ?? "GLTFAnimation";

			// needed because Animator component is unavailable at runtime
			clip.legacy = true;

			foreach (var channel in animation.Channels)
			{
				AnimationCurve[] curves = channel.AsAnimationCurves();

				string nodePath = GetFullNodePath(channel.Target.Node.Id, sceneObj);

				if (channel.Target.Path == GLTFAnimationChannelPath.translation)
				{
					clip.SetCurve(nodePath, typeof(Transform), "localPosition.x", curves[0]);
					clip.SetCurve(nodePath, typeof(Transform), "localPosition.y", curves[1]);
					clip.SetCurve(nodePath, typeof(Transform), "localPosition.z", curves[2]);
				}
				else if (channel.Target.Path == GLTFAnimationChannelPath.rotation)
				{
					clip.SetCurve(nodePath, typeof(Transform), "localRotation.x", curves[0]);
					clip.SetCurve(nodePath, typeof(Transform), "localRotation.y", curves[1]);
					clip.SetCurve(nodePath, typeof(Transform), "localRotation.z", curves[2]);
					clip.SetCurve(nodePath, typeof(Transform), "localRotation.w", curves[3]);
				}
				else if (channel.Target.Path == GLTFAnimationChannelPath.scale)
				{
					clip.SetCurve(nodePath, typeof(Transform), "localScale.x", curves[0]);
					clip.SetCurve(nodePath, typeof(Transform), "localScale.y", curves[1]);
					clip.SetCurve(nodePath, typeof(Transform), "localScale.z", curves[2]);
				}
				else if (channel.Target.Path == GLTFAnimationChannelPath.weights)
				{
					var primitives = channel.Target.Node.Value.Mesh.Value.Primitives;
					var targetCount = primitives[0].Targets.Count;
					for(int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
					{
						string primitiveObjPath = nodePath + "/Primitive" + primitiveIndex;
						for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
						{
							clip.SetCurve(primitiveObjPath, typeof(SkinnedMeshRenderer), "blendShape."+targetIndex, curves[targetIndex]);
						}
					}
				}
				else
				{
					Debug.LogWarning("Cannot read GLTF animation path");
				}
			}

			var a = sceneObj.GetComponent<UnityEngine.Animation>();

			if (a == null)
				a = sceneObj.AddComponent<UnityEngine.Animation>();

			string name = animation.Name ?? ("Animation " + a.GetClipCount());
			a.AddClip(clip, "Animation " + a.GetClipCount());
		}

		private string GetFullNodePath(int nodeId, GameObject sceneObj) {
			GameObject node = _nodeMap[nodeId];
			string nodePath = "";
			do
			{
				nodePath = node.name + (nodePath == "" ? nodePath : "/" + nodePath);
				node = node.transform.parent.gameObject;
			} while (node != null && node != sceneObj);

			return nodePath;
		}

		protected virtual void CreateNodePrimitives(Node node, GameObject nodeObj)
		{
			if (node.Mesh != null)
			{
				for(int i = 0; i < node.Mesh.Value.Primitives.Count; i++)
				{
					var primitive = node.Mesh.Value.Primitives[i];
					var primitiveObj = new GameObject("Primitive" + i);
					primitiveObj.transform.SetParent(nodeObj.transform);
					primitiveObj.transform.localPosition = Vector3.zero;
					primitiveObj.transform.localRotation = Quaternion.identity;
					primitiveObj.transform.localScale = Vector3.one;

					CreateMeshRenderer(primitive, primitiveObj, node.Skin != null ? node.Skin.Value : null);

					primitiveObj.SetActive(true);
				}
			}

			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					CreateNodePrimitives(child.Value, _nodeMap[child.Id]);
				}
			}
		}

		protected void CreateMeshRenderer(MeshPrimitive primitive, GameObject primitiveObj, Skin skin)
		{
			var material = CreateMaterial(
				primitive.Material != null ? primitive.Material.Value : DefaultMaterial,
				primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
			);

			var vertexCount = primitive.Attributes[SemanticProperties.POSITION].Value.Count;

			if (primitive.Contents == null)
			{
				primitive.Contents = new UnityEngine.Mesh
				{
					vertices = primitive.Attributes[SemanticProperties.POSITION].Value.AsVertexArray(),

					normals = primitive.Attributes.ContainsKey(SemanticProperties.NORMAL)
						? primitive.Attributes[SemanticProperties.NORMAL].Value.AsNormalArray()
						: null,

					uv = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(0))
						? primitive.Attributes[SemanticProperties.TexCoord(0)].Value.AsTexcoordArray()
						: null,

					uv2 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(1))
						? primitive.Attributes[SemanticProperties.TexCoord(1)].Value.AsTexcoordArray()
						: null,

					uv3 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(2))
						? primitive.Attributes[SemanticProperties.TexCoord(2)].Value.AsTexcoordArray()
						: null,

					uv4 = primitive.Attributes.ContainsKey(SemanticProperties.TexCoord(3))
						? primitive.Attributes[SemanticProperties.TexCoord(3)].Value.AsTexcoordArray()
						: null,

					colors = primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
						? primitive.Attributes[SemanticProperties.Color(0)].Value.AsColorArray()
						: null,

					triangles = primitive.Indices != null
						? primitive.Indices.Value.AsTriangles()
						: MeshPrimitive.GenerateTriangles(vertexCount),

					tangents = primitive.Attributes.ContainsKey(SemanticProperties.TANGENT)
						? primitive.Attributes[SemanticProperties.TANGENT].Value.AsTangentArray()
						: null,

					boneWeights = primitive.Attributes.ContainsKey(SemanticProperties.Weight(0)) && primitive.Attributes.ContainsKey(SemanticProperties.Joint(0))
						? CreateBoneWeightArray(primitive.Attributes[SemanticProperties.Joint(0)].Value.AsVector4Array(), 
							primitive.Attributes[SemanticProperties.Weight(0)].Value.AsVector4Array(), vertexCount)
						: null
				};
			}

			if(NeedsSkinnedMeshRenderer(primitive, skin))
			{
				var skinnedMeshRenderer = primitiveObj.AddComponent<SkinnedMeshRenderer>();
				skinnedMeshRenderer.material = material;
				skinnedMeshRenderer.quality = SkinQuality.Bone4;

				if (HasBlendShapes(primitive))
					SetupBlendShapes(primitive);

				if (HasBones(skin))
					SetupBones(skin, primitive, skinnedMeshRenderer, primitiveObj);

				skinnedMeshRenderer.sharedMesh = primitive.Contents;
			}
			else
			{
				var meshFilter = primitiveObj.AddComponent<MeshFilter>();
				var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
				meshFilter.sharedMesh = primitive.Contents;
				meshRenderer.material = material;
			}
		}

		bool NeedsSkinnedMeshRenderer(MeshPrimitive primitive, Skin skin)
		{
			return HasBones(skin) || HasBlendShapes(primitive);
		}

		bool HasBones(Skin skin)
		{
			return skin != null;
		}

		void SetupBones(Skin skin, MeshPrimitive primitive, SkinnedMeshRenderer renderer, GameObject primitiveObj)
		{
			var boneCount = skin.Joints.Count;
			Transform[] bones = new Transform[boneCount];
			Matrix4x4[] bindPoses = skin.InverseBindMatrices.Value.AsMatrix4x4Array();

			Matrix4x4 rightToLeftHanded = new Matrix4x4();
			rightToLeftHanded.SetRow(0, new Vector4(1, 0, 0, 0));
			rightToLeftHanded.SetRow(1, new Vector4(0, 1, 0, 0));
			rightToLeftHanded.SetRow(2, new Vector4(0, 0, -1, 0));
			rightToLeftHanded.SetRow(3, new Vector4(0, 0, 0, 1));

			for (int i = 0; i < boneCount; i++)
			{
				bones[i] = _nodeMap[skin.Joints[i].Id].transform;
				bindPoses[i] = rightToLeftHanded.inverse * bindPoses[i] * rightToLeftHanded;
			}

			renderer.rootBone = _nodeMap[skin.Skeleton.Id].transform;
			primitive.Contents.bindposes = bindPoses;
			renderer.bones = bones;
		}

		bool HasBlendShapes(MeshPrimitive primitive)
		{
			return primitive.Targets != null;
		}

		void SetupBlendShapes(MeshPrimitive primitive)
		{
			var mesh = primitive.Contents;
			for (int blendShapeIndex = 0; blendShapeIndex < primitive.Targets.Count; blendShapeIndex++)
			{
				var fieldToAccessor = primitive.Targets[blendShapeIndex];
				var verts = fieldToAccessor["POSITION"].Value.AsVertexArray();
				var normals = fieldToAccessor["NORMAL"].Value.AsNormalArray();
				var tangents = fieldToAccessor["TANGENT"].Value.AsVector3Array();
				mesh.AddBlendShapeFrame(blendShapeIndex.ToString(), 1.0f, verts, normals, tangents);
			}
		}

		public BoneWeight[] CreateBoneWeightArray(Vector4[] joints, Vector4[] weights, int vertCount)
		{
			MakeSureWeightsAddToOne(weights);

			var boneWeights = new BoneWeight[vertCount];
			for(int i = 0; i < vertCount; i++)
			{
				boneWeights[i].boneIndex0 = (int)joints[i].x;
				boneWeights[i].boneIndex1 = (int)joints[i].y;
				boneWeights[i].boneIndex2 = (int)joints[i].z;
				boneWeights[i].boneIndex3 = (int)joints[i].w;

				boneWeights[i].weight0 = weights[i].x;
				boneWeights[i].weight1 = weights[i].y;
				boneWeights[i].weight2 = weights[i].z;
				boneWeights[i].weight3 = weights[i].w;
			}

			return boneWeights;
		}

		void MakeSureWeightsAddToOne(Vector4[] weights) {
			for(int i = 0; i < weights.Length; i++)
			{
				var weightSum = (weights[i].x + weights[i].y + weights[i].z + weights[i].w);
				if (weightSum < 0.98f)
					weights[i] /= weightSum;
			}
		}

		protected virtual UnityEngine.Material CreateMaterial(Material def, bool useVertexColors)
		{
			if (def.ContentsWithVC == null || def.ContentsWithoutVC == null)
			{
				Shader shader;

				// get the shader to use for this material
				try
				{
					if (def.PbrMetallicRoughness != null)
						shader = _shaderCache[MaterialType.PbrMetallicRoughness];
					else if (_root.ExtensionsUsed != null && _root.ExtensionsUsed.Contains("KHR_materials_common")
					         && def.CommonConstant != null)
						shader = _shaderCache[MaterialType.CommonConstant];
					else
						shader = _shaderCache[MaterialType.PbrMetallicRoughness];
				}
				catch (KeyNotFoundException e)
				{
					Debug.LogWarningFormat("No shader supplied for type of glTF material {0}, using Standard fallback", def.Name);
					shader = Shader.Find("Standard");
				}

				shader.maximumLOD = MaximumLod;

				var material = new UnityEngine.Material(shader);

				if (def.AlphaMode == AlphaMode.MASK)
				{
					material.SetOverrideTag("RenderType", "TransparentCutout");
					material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.AlphaTest;
					material.SetFloat("_Cutoff", (float) def.AlphaCutoff);
				}
				else if (def.AlphaMode == AlphaMode.BLEND)
				{
					material.SetOverrideTag("RenderType", "Transparent");
					material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
				}
				else
				{
					material.SetOverrideTag("RenderType", "Opaque");
					material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
				}

				if (def.DoubleSided)
				{
					material.SetInt("_Cull", (int) CullMode.Off);
				}
				else
				{
					material.SetInt("_Cull", (int) CullMode.Back);
				}

				if (def.PbrMetallicRoughness != null)
				{
					var pbr = def.PbrMetallicRoughness;

					material.SetColor("_Color", pbr.BaseColorFactor);

					if (pbr.BaseColorTexture != null)
					{
						var texture = pbr.BaseColorTexture.Index.Value;
						material.SetTexture("_MainTex", CreateTexture(texture));
					}

					material.SetFloat("_Metallic", (float) pbr.MetallicFactor);

					if (pbr.MetallicRoughnessTexture != null)
					{
						var texture = pbr.MetallicRoughnessTexture.Index.Value;
						material.SetTexture("_MetallicGlossMap", CreateTexture(texture));
					}

					material.SetFloat("_Glossiness", (float) (1-pbr.RoughnessFactor));
				}

				if (def.CommonConstant != null)
				{
					material.SetColor("_AmbientFactor", def.CommonConstant.AmbientFactor);

					if (def.CommonConstant.LightmapTexture != null)
					{
						material.EnableKeyword("LIGHTMAP_ON");

						var texture = def.CommonConstant.LightmapTexture.Index.Value;
						material.SetTexture("_LightMap", CreateTexture(texture));
						material.SetInt("_LightUV", def.CommonConstant.LightmapTexture.TexCoord);
					}

					material.SetColor("_LightFactor", def.CommonConstant.LightmapFactor);
				}

				if (def.NormalTexture != null)
				{
					var texture = def.NormalTexture.Index.Value;
					material.SetTexture("_BumpMap", CreateTexture(texture));
					material.SetFloat("_BumpScale", (float) def.NormalTexture.Scale);
				}

				if (def.OcclusionTexture != null)
				{
					var texture = def.OcclusionTexture.Index;

					material.SetFloat("_OcclusionStrength", (float) def.OcclusionTexture.Strength);

					if (def.PbrMetallicRoughness != null
					    && def.PbrMetallicRoughness.MetallicRoughnessTexture != null
					    && def.PbrMetallicRoughness.MetallicRoughnessTexture.Index.Id == texture.Id)
					{
						material.EnableKeyword("OCC_METAL_ROUGH_ON");
					}
					else
					{
						material.SetTexture("_OcclusionMap", CreateTexture(texture.Value));
					}
				}

				if (def.EmissiveTexture != null)
				{
					var texture = def.EmissiveTexture.Index.Value;
					material.EnableKeyword("EMISSION_MAP_ON");
					material.SetTexture("_EmissionMap", CreateTexture(texture));
					material.SetInt("_EmissionUV", def.EmissiveTexture.TexCoord);
				}

				material.SetColor("_EmissionColor", def.EmissiveFactor);

				def.ContentsWithoutVC = material;
				def.ContentsWithVC = new UnityEngine.Material(material);
				def.ContentsWithVC.EnableKeyword("VERTEX_COLOR_ON");
			}

			return def.GetContents(useVertexColors);
		}

		protected virtual UnityEngine.Texture CreateTexture(Texture texture)
		{
			if (texture.Contents)
				return texture.Contents;

			var source = texture.Source.Value.Contents;
			var desiredFilterMode = FilterMode.Bilinear;
			var desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;

			if (texture.Sampler != null)
			{
				var sampler = texture.Sampler.Value;
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
					case GLTF.WrapMode.ClampToEdge:
						desiredWrapMode = UnityEngine.TextureWrapMode.Clamp;
						break;
					case GLTF.WrapMode.Repeat:
					default:
						desiredWrapMode = UnityEngine.TextureWrapMode.Repeat;
						break;
				}
			}

			if (source.filterMode == desiredFilterMode && source.wrapMode == desiredWrapMode)
			{
				texture.Contents = source;
			}
			else
			{
				texture.Contents = UnityEngine.Object.Instantiate(source);
				texture.Contents.filterMode = desiredFilterMode;
				texture.Contents.wrapMode = desiredWrapMode;
			}

			return texture.Contents;
		}

		protected const string Base64StringInitializer = "^data:[a-z-]+/[a-z-]+;base64,";

		/// <summary>
		///  Get the absolute path to a gltf uri reference.
		/// </summary>
		/// <param name="relativePath">The relative path stored in the uri.</param>
		/// <returns></returns>
		protected virtual string AbsolutePath(string relativePath)
		{
			var uri = new Uri(_gltfUrl);
			var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length);
			return partialPath + relativePath;
		}

		protected virtual IEnumerator LoadImage(Image image)
		{
			Texture2D texture;

			if (image.Uri != null)
			{
				var uri = image.Uri;

				Regex regex = new Regex(Base64StringInitializer);
				Match match = regex.Match(uri);
				if (match.Success)
				{
					var base64Data = uri.Substring(match.Length);
					var textureData = Convert.FromBase64String(base64Data);
					texture = new Texture2D(0, 0);
					texture.LoadImage(textureData);
				}
				else
				{
					var www = UnityWebRequest.Get(AbsolutePath(uri));
					www.downloadHandler = new DownloadHandlerTexture();

					yield return www.Send();

					// HACK to enable mipmaps :(
					var tempTexture = DownloadHandlerTexture.GetContent(www);
					if (tempTexture != null)
					{
						texture = new Texture2D(tempTexture.width, tempTexture.height, tempTexture.format, true);
						texture.SetPixels(tempTexture.GetPixels());
						texture.Apply(true);
					}
					else
					{
						Debug.LogFormat("{0} {1}", www.responseCode, www.url);
						texture = new Texture2D(16, 16);
					}
				}
			}
			else
			{
				texture = new Texture2D(0, 0);
				var bufferView = image.BufferView.Value;
				var buffer = bufferView.Buffer.Value;
				var data = new byte[bufferView.ByteLength];
				System.Buffer.BlockCopy(buffer.Contents, bufferView.ByteOffset, data, 0, data.Length);
				texture.LoadImage(data);
			}

			image.Contents = texture;
		}

		/// <summary>
		/// Load the remote URI data into a byte array.
		/// </summary>
		protected virtual IEnumerator LoadBuffer(Buffer buffer)
		{
			if (buffer.Uri != null)
			{
				byte[] bufferData;
				var uri = buffer.Uri;

				Regex regex = new Regex(Base64StringInitializer);
				Match match = regex.Match(uri);
				if (match.Success)
				{
					var base64Data = uri.Substring(match.Length);
					bufferData = Convert.FromBase64String(base64Data);
				}
				else
				{
					var www = UnityWebRequest.Get(AbsolutePath(uri));

					yield return www.Send();

					bufferData = www.downloadHandler.data;
				}

				buffer.Contents = bufferData;
			}
		}

		public virtual void Dispose()
		{
			foreach (var mesh in _root.Meshes)
			{
				foreach (var prim in mesh.Primitives)
				{
					GameObject.Destroy(prim.Contents);
				}
			}
		}
	}
}
