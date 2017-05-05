using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;
using GLTF.Extensions;

namespace GLTF
{
    public class GLTFLoader
    {
	    public bool Multithreaded = true;
		private readonly string _gltfUrl;
        private GLTFRoot _root;
        private AsyncAction asyncAction;
	    private readonly Transform _sceneParent;
        private readonly Dictionary<GLTFBuffer, byte[]> _bufferCache = new Dictionary<GLTFBuffer, byte[]>();
        private readonly Dictionary<MaterialCacheKey, Material> _materialCache = new Dictionary<MaterialCacheKey, Material>();
        private readonly Dictionary<GLTFImage, Texture2D> _imageCache = new Dictionary<GLTFImage, Texture2D>();
        private readonly Dictionary<GLTFMesh, GameObject> _meshCache = new Dictionary<GLTFMesh, GameObject>();
        private readonly Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes> _attributesCache = new Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes>();

        private struct MaterialCacheKey
        {
            public GLTFMaterial Material;
            public bool UseVertexColors;
        }

        public GLTFLoader(string gltfUrl, Transform parent = null)
		{
            _gltfUrl = gltfUrl;
	        _sceneParent = parent;
            asyncAction = new AsyncAction();

            var specGlossExtension = new KHRMaterialPBRSpecularGlossinessFactory();
            GLTFMaterial.RegisterExtension(specGlossExtension);
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

            var materialCacheKey = new MaterialCacheKey {
                Material = primitive.Material.Value,
                UseVertexColors = attributes.Colors != null
            };
            meshRenderer.material = FindOrCreateMaterial(materialCacheKey);

            return primitiveObj;
        }

        private Material FindOrCreateMaterial(MaterialCacheKey materialKey)
        {
            Material material;

            if (_materialCache.TryGetValue(materialKey, out material))
            {
                return material;
            }

	        material = CreateMaterial(materialKey.Material, materialKey.UseVertexColors);

			_materialCache.Add(materialKey, material);

	        return material;
        }

        private Material CreateMaterial(GLTFMaterial def, bool useVertexColors)
        {

            GLTFExtension extension;
            if (def.Extensions.TryGetValue(KHRMaterialPBRSpecularGlossiness.ExtensionName, out extension))
            {
                var specDef = (KHRMaterialPBRSpecularGlossiness) extension;

                return CreateSpecularMaterial(def, specDef, useVertexColors);
            }

            Shader shader;

            if (def.AlphaMode == GLTFAlphaMode.OPAQUE)
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFStandardDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFStandard");
                }
            }
            else if (def.AlphaMode == GLTFAlphaMode.MASK)
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFStandardTransparentMaskDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFStandardTransparentMask");
                }
            }
            else
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFStandardTransparentBlendDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFStandardTransparentBlend");
                }
            }

            var material = new Material(shader);

            if (useVertexColors)
            {
                Debug.Log("Enabling vertex colors.");
                material.EnableKeyword("VERTEX_COLOR_ON");
            }

            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                material.SetColor("_Color", pbr.BaseColorFactor);

                material.SetFloat("_Cutoff", (float)def.AlphaCutoff);

                if (pbr.BaseColorTexture != null)
                {
                    var texture = pbr.BaseColorTexture.Index.Value;
                    material.SetTexture("_MainTex", _imageCache[texture.Source.Value]);
                }

                material.SetFloat("_Metallic", (float)pbr.MetallicFactor);

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var texture = pbr.MetallicRoughnessTexture.Index.Value;
                    material.SetTexture("_MetallicRoughness", _imageCache[texture.Source.Value]);
                }

                material.SetFloat("_Roughness", (float)pbr.RoughnessFactor);
            }

            if (def.NormalTexture != null)
            {
                var texture = def.NormalTexture.Index.Value;
                material.SetTexture("_BumpMap", _imageCache[texture.Source.Value]);
                material.SetFloat("_Bump", (float)def.NormalTexture.Scale);
            }

            if (def.OcclusionTexture != null)
            {
                var texture = def.OcclusionTexture.Index.Value;
                material.SetTexture("_AOTex", _imageCache[texture.Source.Value]);
                material.SetFloat("_Occlusion", (float)def.OcclusionTexture.Strength);
            }

            if (def.EmissiveTexture != null)
            {
                var texture = def.EmissiveTexture.Index.Value;
                material.SetTexture("_EmissionTex", _imageCache[texture.Source.Value]);
            }

            material.SetColor("_Emission", def.EmissiveFactor);

            return material;
        }

        private Material CreateSpecularMaterial(GLTFMaterial def, KHRMaterialPBRSpecularGlossiness specDef, bool useVertexColors)
        {
            Shader shader;

            if (def.AlphaMode == GLTFAlphaMode.OPAQUE)
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFSpecularDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFSpecular");
                }
            }
            else if (def.AlphaMode == GLTFAlphaMode.MASK)
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFSpecularTransparentMaskDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFSpecularTransparentMask");
                }
            }
            else
            {
                if (def.DoubleSided)
                {
                    shader = Shader.Find("GLTF/GLTFSpecularTransparentBlendDoubleSided");
                }
                else
                {
                    shader = Shader.Find("GLTF/GLTFSpecularTransparentBlend");
                }
            }

            var material = new Material(shader);

            if (useVertexColors)
            {
                Debug.Log("Enabling vertex colors.");
                material.EnableKeyword("VERTEX_COLOR_ON");
            }

            material.SetColor("_Diffuse", specDef.DiffuseFactor);

            material.SetFloat("_Cutoff", (float)def.AlphaCutoff);

            if (specDef.DiffuseTexture != null)
            {
                var texture = specDef.DiffuseTexture.Index.Value;
                material.SetTexture("_MainTex", _imageCache[texture.Source.Value]);
            }

            material.SetFloat("_Glossiness", (float)specDef.GlossinessFactor);
            material.SetColor("_Specular", specDef.SpecularFactor);

            if (specDef.SpecularGlossinessTexture != null)
            {
                var texture = specDef.SpecularGlossinessTexture.Index.Value;
                material.SetTexture("_SpecularGlossinessTex", _imageCache[texture.Source.Value]);
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
                material.SetFloat("_Occlusion", (float)def.OcclusionTexture.Strength);
            }

            if (def.EmissiveTexture != null)
            {
                var texture = def.EmissiveTexture.Index.Value;
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
