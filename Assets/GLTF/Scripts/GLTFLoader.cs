using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;

namespace GLTF
{
    public class GLTFLoader
    {
	    public bool Multithreaded = true;

	    private Shader _shader;
		private readonly string _gltfUrl;
        private GLTFRoot _root;
        private AsyncAction asyncAction;
	    private readonly Transform _sceneParent;
        private readonly Dictionary<GLTFBuffer, byte[]> _bufferCache = new Dictionary<GLTFBuffer, byte[]>();
        private readonly Dictionary<GLTFMaterial, Material> _materialCache = new Dictionary<GLTFMaterial, Material>();
        private readonly Dictionary<GLTFImage, Texture2D> _imageCache = new Dictionary<GLTFImage, Texture2D>();
        private readonly Dictionary<GLTFMesh, GameObject> _meshCache = new Dictionary<GLTFMesh, GameObject>();
        private readonly Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes> _attributesCache = new Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes>();

        public GLTFLoader(string gltfUrl)
        {
            _gltfUrl = gltfUrl;
            _shader = Shader.Find("GLTF/GLTFStandardShader");
            asyncAction = new AsyncAction();
        }

        public GLTFLoader(string gltfUrl, Shader shader, Transform parent = null)
		{
            _gltfUrl = gltfUrl;
	        _sceneParent = parent;
			_shader = shader;
            asyncAction = new AsyncAction();
		}

        public IEnumerator Load()
        {
            var www = UnityWebRequest.Get(_gltfUrl);

            yield return www.Send();

            var gltfData = www.downloadHandler.data;

	        if (Multithreaded)
	        {
		        yield return asyncAction.RunOnWorkerThread(() => ParseGLTF(gltfData));
	        }
	        else
	        {
                ParseGLTF(gltfData);
	        }

            var scene = _root.GetDefaultScene();

            if (scene == null)
            {
                throw new Exception("No default scene in gltf file.");
            }

            foreach (var buffer in _root.Buffers)
            {
                yield return LoadBuffer(buffer);
            }

            foreach (var image in _root.Images)
            {
                yield return LoadImage(image);
            }

	        if (Multithreaded)
	        {
		        yield return asyncAction.RunOnWorkerThread(() => BuildMeshAttributes());
	        }
	        else
	        {
		        BuildMeshAttributes();
	        }

            var sceneObj = CreateScene(scene);

	        if (_sceneParent != null)
	        {
		        sceneObj.transform.SetParent(_sceneParent, false);
	        }

			_root = null;
        }

        private void ParseGLTF(byte[] gltfData)
        {
            byte[] glbBuffer;
            _root = GLTFParser.ParseBinary(gltfData, out glbBuffer);

            if (glbBuffer != null)
            {
                _bufferCache[_root.Buffers[0]] = glbBuffer;
            }
        }

        private void BuildMeshAttributes()
        {
            foreach (var mesh in _root.Meshes)
            {
                foreach (var primitive in mesh.Primitives)
                {
                    var attributes = primitive.BuildMeshAttributes(_bufferCache);
                    _attributesCache[primitive] = attributes;
                }
            }
        }

        private GameObject CreateScene(GLTFScene scene)
        {
            var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

            foreach (var node in scene.Nodes)
            {
				var nodeObj = CreateNode(node.Value);
	            nodeObj.transform.SetParent(sceneObj.transform, false);
			}

            return sceneObj;
        }

        private GameObject CreateNode(GLTFNode node)
        {
            var nodeObj = new GameObject(node.Name ?? "GLTFNode");

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
                var meshObj = FindOrCreateMeshObject(node.Mesh.Value);
	            meshObj.transform.SetParent(nodeObj.transform, false);
			}

            /* TODO: implement camera (probably a flag to disable for VR as well)
            if (camera != null)
            {
                GameObject cameraObj = camera.Value.Create();
                cameraObj.transform.parent = nodeObj.transform;
            }
            */

            foreach (var child in node.Children)
            {
                var childObj = CreateNode(child.Value);
	            childObj.transform.SetParent(nodeObj.transform, false);
			}

            return nodeObj;
        }

        private GameObject FindOrCreateMeshObject(GLTFMesh mesh)
        {
            GameObject meshObj;

			if (_meshCache.TryGetValue(mesh, out meshObj))
            {
                return GameObject.Instantiate(meshObj);
            }

            meshObj = CreateMeshObject(mesh);

			_meshCache.Add(mesh, meshObj);

	        return meshObj;
        }

        private GameObject CreateMeshObject(GLTFMesh mesh)
        {
            var meshName = mesh.Name ?? "GLTFMesh";
            var meshObj = new GameObject(meshName);

            foreach (var primitive in mesh.Primitives)
            {
                var primitiveObj = CreateMeshPrimitive(primitive);
	            primitiveObj.transform.SetParent(meshObj.transform, false);
			}

            return meshObj;
        }

        private GameObject CreateMeshPrimitive(GLTFMeshPrimitive primitive)
        {
            var primitiveObj = new GameObject("Primitive");
	        
			var meshFilter = primitiveObj.AddComponent<MeshFilter>();

            var attributes = _attributesCache[primitive];

            var mesh = new Mesh
            {
                vertices = attributes.Vertices,
                normals = attributes.Normals,
                uv = attributes.Uv,
                uv2 = attributes.Uv2,
                uv3 = attributes.Uv3,
                uv4 = attributes.Uv4,
                colors = attributes.Colors,
                triangles = attributes.Triangles,
                tangents = attributes.Tangents
            };

            meshFilter.mesh = mesh;

            var meshRenderer = primitiveObj.AddComponent<MeshRenderer>();

            meshRenderer.material = FindOrCreateMaterial(primitive.Material.Value);

            return primitiveObj;
        }

        private Material FindOrCreateMaterial(GLTFMaterial gltfMaterial)
        {
            Material material;

            if (_materialCache.TryGetValue(gltfMaterial, out material))
            {
                return material;
            }

	        material = CreateMaterial(gltfMaterial);

			_materialCache.Add(gltfMaterial, material);

	        return material;
        }

        private Material CreateMaterial(GLTFMaterial def)
        {
            var material = new Material(_shader);

            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                material.SetColor("_Color", pbr.BaseColorFactor);

                if (pbr.BaseColorTexture != null)
                {
                    var texture = pbr.BaseColorTexture.Index.Value;
                    material.SetTexture("_MainTex", _imageCache[texture.Source.Value]);
                    material.SetTextureScale("_MainTex", new Vector2(1, 1));
                }

                material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var texture = pbr.MetallicRoughnessTexture.Index.Value;
                    material.SetTexture("_MetallicRoughness", _imageCache[texture.Source.Value]);
                    material.SetTextureScale("_MetallicRoughness", new Vector2(1, 1));
                }

                material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
            }

            if (def.NormalTexture != null)
            {
                var texture = def.NormalTexture.Index.Value;
                material.SetTexture("_BumpMap", _imageCache[texture.Source.Value]);
                material.SetTextureScale("_BumpMap", new Vector2(1, 1));
                material.SetFloat("_Bump", (float)def.NormalTexture.Scale);
            }

            if (def.OcclusionTexture != null)
            {
                var texture = def.OcclusionTexture.Index.Value;
                material.SetTexture("_AOTex", _imageCache[texture.Source.Value]);
                material.SetTextureScale("_AOTex", new Vector2(1, 1));
                material.SetFloat("_Occlusion", (float)def.OcclusionTexture.Strength);
            }

            if (def.EmissiveTexture != null)
            {
                var texture = def.EmissiveTexture.Index.Value;
                material.SetTextureScale("_EmissionTex", new Vector2(1, 1));
                material.SetTexture("_EmissionTex", _imageCache[texture.Source.Value]);
            }

            material.SetColor("_Emission", def.EmissiveFactor);

            return material;
        }

	    private const string Base64StringInitializer = "data:application/octet-stream;base64,";

	    /// <summary>
        ///  Get the absolute path to a gltf uri reference.
        /// </summary>
        /// <param name="relativePath">The relative path stored in the uri.</param>
        /// <returns></returns>
        private string AbsolutePath(string relativePath)
        {
            var uri = new Uri(_gltfUrl);
            var partialPath = uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length - 1].Length);
            return partialPath + relativePath;
        }

        private IEnumerator LoadImage(GLTFImage image)
        {
            Texture2D texture;

            if (image.Uri != null)
            {
                var uri = image.Uri;

                if (uri.StartsWith(Base64StringInitializer))
                {
                    var base64Data = uri.Substring(Base64StringInitializer.Length);
                    var textureData = Convert.FromBase64String(base64Data);
                    texture = new Texture2D(0, 0);
                    texture.LoadImage(textureData);
                }
                else
                {
                    var www = UnityWebRequest.Get(AbsolutePath(uri));
                    www.downloadHandler = new DownloadHandlerTexture();

                    yield return www.Send();

                    texture = DownloadHandlerTexture.GetContent(www);
                }
            }
            else
            {
                texture = new Texture2D(0, 0);
                var bufferView = image.BufferView.Value;
                var buffer = bufferView.Buffer.Value;
                var bufferData = _bufferCache[buffer];
				var data = new byte[bufferView.ByteLength];
				Buffer.BlockCopy(bufferData, bufferView.ByteOffset, data, 0, data.Length);
                texture.LoadImage(data);
            }

            _imageCache[image] = texture;
        }

        /// <summary>
        /// Load the remote URI data into a byte array.
        /// </summary>
        private IEnumerator LoadBuffer(GLTFBuffer buffer)
        {
			if (buffer.Uri != null)
			{
				byte[] bufferData;
				var uri = buffer.Uri;

                if (uri.StartsWith(Base64StringInitializer))
                {
                    var base64Data = uri.Substring(Base64StringInitializer.Length);
                    bufferData = Convert.FromBase64String(base64Data);
                }
                else
                {
                    var www = UnityWebRequest.Get(AbsolutePath(uri));

                    yield return www.Send();

                    bufferData = www.downloadHandler.data;
                }

	            _bufferCache[buffer] = bufferData;
			}
        }
    }
}
