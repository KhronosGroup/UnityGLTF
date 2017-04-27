using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace GLTF
{
    public class GLTFLoader
    {
        private readonly string _gltfUrl;
        private GLTFRoot _root;
	    private readonly Transform _sceneParent;
	    private readonly bool _multithreaded;
        private bool _workerThreadRunning = false;
        private readonly Dictionary<GLTFBuffer, byte[]> _bufferCache = new Dictionary<GLTFBuffer, byte[]>();
        private readonly Dictionary<GLTFMaterial, Material> _materialCache = new Dictionary<GLTFMaterial, Material>();
        private readonly Dictionary<GLTFImage, Texture2D> _imageCache = new Dictionary<GLTFImage, Texture2D>();
        private readonly Dictionary<GLTFMesh, GameObject> _meshCache = new Dictionary<GLTFMesh, GameObject>();
        private readonly Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes> _attributesCache = new Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes>();

        public GLTFLoader(string gltfUrl, Transform parent = null, bool multithreaded = true)
		{
            _gltfUrl = gltfUrl;
	        _sceneParent = parent;
			_multithreaded = multithreaded;
		}

        public IEnumerator Load()
        {
            var www = UnityWebRequest.Get(_gltfUrl);

            yield return www.Send();

            var gltfData = www.downloadHandler.data;

	        if (_multithreaded)
	        {
		        yield return ParseGLTFAsync(gltfData);
	        }
	        else
	        {
		        _root = ParseGLTF(gltfData);
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

	        if (_multithreaded)
	        {
		        yield return BuildMeshAttributesAsync();
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

        private IEnumerator ParseGLTFAsync(byte[] gltfData)
        {
            _workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                _root = ParseGLTF(gltfData);

                _workerThreadRunning = false;
            });

            yield return Wait();
        }

        private enum ChunkFormat : uint
        {
            JSON = 0x4e4f534a,
            BIN = 0x004e4942
        }

        private GLTFRoot ParseGLTF(byte[] gltfBinary)
        {
            string gltfContent;
            byte[] gltfBinaryChunk = null;

            // Check for binary format magic bytes
            if (BitConverter.ToUInt32(gltfBinary, 0) == 0x46546c67)
            {
                // Parse header information

                var version = BitConverter.ToUInt32(gltfBinary, 4);
                if (version != 2)
                {
                    throw new GLTFHeaderInvalidException("Unsupported glTF version");
                }

                var length = BitConverter.ToUInt32(gltfBinary, 8);
                if (length != gltfBinary.Length)
                {
                    throw new GLTFHeaderInvalidException("File length does not match header.");
                }

                var chunkLength = BitConverter.ToUInt32(gltfBinary, 12);
                var chunkType = BitConverter.ToUInt32(gltfBinary, 16);
                if (chunkType != (uint)ChunkFormat.JSON)
                {
                    throw new GLTFHeaderInvalidException("First chunk must be of type JSON");
                }

                // Load JSON chunk
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary, 20, (int)chunkLength);

                // Load Binary Chunk
                if (20 + chunkLength < length)
                {
                    var start = 20 + (int)chunkLength;
                    chunkLength = BitConverter.ToUInt32(gltfBinary, start);
                    if (start + chunkLength > length)
                    {
                        throw new GLTFHeaderInvalidException("File length does not match chunk header.");
                    }

                    chunkType = BitConverter.ToUInt32(gltfBinary, start + 4);
                    if (chunkType != (uint)ChunkFormat.BIN)
                    {
                        throw new GLTFHeaderInvalidException("Second chunk must be of type BIN if present");
                    }

                    gltfBinaryChunk = new byte[chunkLength];
                    Buffer.BlockCopy(gltfBinary, start + 8, gltfBinaryChunk, 0, (int)chunkLength);
                }
            }
            else
            {
                gltfContent = System.Text.Encoding.UTF8.GetString(gltfBinary);
            }

            var stringReader = new StringReader(gltfContent);
            var root = GLTFRoot.Deserialize(new JsonTextReader(stringReader));

            if (gltfBinaryChunk != null)
            {
                if (root.Buffers == null || root.Buffers.Count == 0)
                {
                    throw new Exception("Binary buffer not defined in buffers array.");
                }

				_bufferCache[root.Buffers[0]] = gltfBinaryChunk;
			}

            return root;
        }

        private IEnumerator BuildMeshAttributesAsync()
        {
            _workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                BuildMeshAttributes();
                _workerThreadRunning = false;
            });

            yield return Wait();
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

        private IEnumerator Wait()
        {
            while (_workerThreadRunning)
            {
                yield return null;
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
			node.GetTRSProperties(out position, out rotation, out scale);
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
            var material = new Material(Shader.Find("GLTF/GLTFStandardShader"));

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
