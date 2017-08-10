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
				if (www.responseCode >= 400)
				{
					Debug.LogErrorFormat("{0} - {1}", www.responseCode, www.url);
					yield break;
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
			Debug.Log("Animation input type " + _root.Animations[0].Samplers[0].Input.Value.Type);
			Debug.Log("Animation input Count " + _root.Animations[0].Samplers[0].Input.Value.Count);
			var floats = _root.Animations[0].Samplers[0].Input.Value.AsFloatArray();
			Debug.Log("Input array");
			foreach (var f in floats)
				Debug.Log(f);

			Debug.Log("Animation output type " + _root.Animations[0].Samplers[0].Output.Value.Type);
			Debug.Log("Animation output Count " + _root.Animations[0].Samplers[0].Output.Value.Count);
			var vecs = _root.Animations[0].Samplers[0].Output.Value.AsVector4Array();
			Debug.Log("Output array");
			foreach (var v in vecs)
				Debug.Log(v);


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
			Dictionary<int, GameObject> nodeMap = new Dictionary<int, GameObject>();

			foreach (var node in scene.Nodes)
			{
				var nodeObj = CreateNode(node, nodeMap);
				nodeObj.transform.SetParent(sceneObj.transform, false);
			}

			// Add animations to scene
			foreach (var animation in _root.Animations)
				AddAnimationToScene(scene, sceneObj, nodeMap, animation);

			return sceneObj;
		}

		protected virtual GameObject CreateNode(NodeId nodeId, Dictionary<int, GameObject> nodeMap)
		{
			var node = nodeId.Value;
			var nodeObj = new GameObject(node.Name ?? ("GLTFNode" + nodeId.Id));
			nodeMap[nodeId.Id] = nodeObj;

			Vector3 position;
			Quaternion rotation;
			Vector3 scale;
			node.GetUnityTRSProperties(out position, out rotation, out scale);
			nodeObj.transform.localPosition = position;
			nodeObj.transform.localRotation = rotation;
			nodeObj.transform.localScale = scale;

			// TODO: Add support for skin/morph targets
			if (node.Mesh != null)
			{
				CreateMeshObject(node.Mesh.Value, nodeObj.transform);
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
					var childObj = CreateNode(child, nodeMap);
					childObj.transform.SetParent(nodeObj.transform, false);
				}
			}

			return nodeObj;
		}

		/// <summary>
		/// Generate Animation components from glTF animations, and attach to game objects
		/// </summary>
		protected void AddAnimationToScene(Scene scene, GameObject sceneObj, Dictionary<int, GameObject> nodeMap, Animation animation)
		{
			// create the animation clip that will contain animation data
			AnimationClip clip = new AnimationClip();
			clip.name = animation.Name ?? "GLTFAnimation";

			//TODO: Necessary?
			// needed because Animator component is unavailable at runtime
			clip.legacy = true;

			foreach (var channel in animation.Channels)
			{
				AnimationCurve[] curves = AsAnimationCurves(channel.Sampler.Value);

				// TODO Memoize
				// Map the target Node ID to a GameObject, get its name, and prepend parents' names to generate a path.
				GameObject node = nodeMap[channel.Target.Node.Id];
				string nodePath = "";
				do
				{
					nodePath = node.name + (nodePath == "" ? nodePath : "/" + nodePath);
					node = node.transform.parent.gameObject;
				} while (node != null && node != sceneObj);

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
				else
				{
					Debug.LogWarning("Cannot read GLTF animation path");
				}
			}

			var a = sceneObj.GetComponent<UnityEngine.Animation>();

			if(a == null)
				a = sceneObj.AddComponent<UnityEngine.Animation>();

			string name = animation.Name ?? ("Animation " + a.GetClipCount());
			a.AddClip(clip, "Animation " + a.GetClipCount());

			if (!a.isPlaying)
				a.Play(name);
		}

		/// <summary>
		/// Create AnimationCurves from glTF animation sampler data
		/// </summary>
		/// <returns>AnimationCurve[]</returns>
		protected AnimationCurve[] AsAnimationCurves(AnimationSampler animationSampler)
		{
			float[] timeArray = animationSampler.Input.Value.AsFloatArray();

			// check transform stride
			int stride = 3;
			if (animationSampler.Output.Value.Type == GLTFAccessorAttributeType.VEC3)
				stride = 3;
			else if (animationSampler.Output.Value.Type == GLTFAccessorAttributeType.VEC4)
				stride = 4;
			else
				throw new GLTFTypeMismatchException("Animation sampler output points to invalidly-typed accessor");

			var curves = new AnimationCurve[stride];

			for (int i = 0; i < stride; i++)
				curves[i] = new AnimationCurve();

			if (animationSampler.Output.Value.Type == GLTFAccessorAttributeType.VEC3) { 
				var animArray = animationSampler.Output.Value.AsVector3Array();
				for (int i = 0; i < stride; i++)
					for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
						curves[i].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx][i]));
			}

			if (animationSampler.Output.Value.Type == GLTFAccessorAttributeType.VEC4)
			{
				var animArray = animationSampler.Output.Value.AsVector4Array();
				for (int i = 0; i < stride; i++)
					for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
						curves[i].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx][i]));
			}

			if(animationSampler.Interpolation == InterpolationType.LINEAR)
				for (int i = 0; i < curves.Length; i++)
					LinearizeCurve(curves[i]);

			if(animationSampler.Interpolation == InterpolationType.STEP)
				for (int i = 0; i < curves.Length; i++)
					StepCurve(curves[i]);

			return curves;
		}

		public void LinearizeCurve(AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
				{
					key.inTangent = CalculateLinearTangent(curve[timeIdx - 1].value, curve[timeIdx].value, curve[timeIdx - 1].time, curve[timeIdx].time);
				}
				if (timeIdx + 1 < curve.length)
				{
					key.outTangent = CalculateLinearTangent(curve[timeIdx].value, curve[timeIdx + 1].value, curve[timeIdx].time, curve[timeIdx + 1].time);
				}
				curve.MoveKey(timeIdx, key);
			}
		}

		public void StepCurve(AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
				{
					key.inTangent = float.PositiveInfinity;
				}
				if (timeIdx + 1 < curve.length)
				{
					key.outTangent = float.PositiveInfinity;
				}
				curve.MoveKey(timeIdx, key);
			}
		}

		private float CalculateLinearTangent (float valueStart, float valueEnd, float timeStart, float timeEnd)
		{
			return (valueEnd - valueStart) / (timeEnd - timeStart);
		}

		protected virtual void CreateMeshObject(Mesh mesh, Transform parent)
		{
			foreach (var primitive in mesh.Primitives)
			{
				var primitiveObj = CreateMeshPrimitive(primitive);
				primitiveObj.transform.SetParent(parent, false);
				primitiveObj.SetActive(true);
			}
		}

		protected virtual GameObject CreateMeshPrimitive(MeshPrimitive primitive)
		{
			var primitiveObj = new GameObject("Primitive");

			var meshFilter = primitiveObj.AddComponent<MeshFilter>();
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
						: null
				};
			}

			meshFilter.sharedMesh = primitive.Contents;

			var material = CreateMaterial(
				primitive.Material != null ? primitive.Material.Value : DefaultMaterial,
				primitive.Attributes.ContainsKey(SemanticProperties.Color(0))
			);

			var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();
			meshRenderer.material = material;

			return primitiveObj;
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
		private object keyframe;

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
