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
        private string _gltfUrl;
        private GLTFRoot _root;
        private bool workerThreadRunning = false;
        private Dictionary<GLTFBuffer, byte[]> _bufferCache = new Dictionary<GLTFBuffer, byte[]>();
        private Dictionary<GLTFMaterial, Material> _materialCache = new Dictionary<GLTFMaterial, Material>();
        private Dictionary<GLTFImage, Texture2D> _imageCache = new Dictionary<GLTFImage, Texture2D>();
        private Dictionary<GLTFMesh, GameObject> _meshCache = new Dictionary<GLTFMesh, GameObject>();
        private Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes> _attributesCache = new Dictionary<GLTFMeshPrimitive, GLTFMeshPrimitiveAttributes>();

        public GLTFLoader(string gltfUrl)
        {
            _gltfUrl = gltfUrl;
        }

        public IEnumerator Load(GameObject parent = null)
        {
            var www = UnityWebRequest.Get(_gltfUrl);

            yield return www.Send();

            var gltfData = www.downloadHandler.data;

            yield return ParseGLTFAsync(gltfData);

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

            yield return BuildMeshAttributesAsync();

            CreateScene(scene, parent);

            _root = null;
        }

        private IEnumerator ParseGLTFAsync(byte[] gltfData)
        {
            workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                _root = ParseGLTF(gltfData);

                workerThreadRunning = false;
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
            workerThreadRunning = true;

            ThreadPool.QueueUserWorkItem((_) =>
            {

                foreach (var mesh in _root.Meshes)
                {
                    foreach (var primitive in mesh.Primitives)
                    {
                        var attributes = BuildMeshAttributes(primitive);
                        _attributesCache[primitive] = attributes;
                    }
                }

                workerThreadRunning = false;
            });

            yield return Wait();
        }

        private IEnumerator Wait()
        {
            while (workerThreadRunning)
            {
                yield return null;
            }
        }

        private GameObject CreateScene(GLTFScene scene, GameObject rootObj = null)
        {
            var sceneObj = new GameObject(scene.Name ?? "GLTFScene");

            if (rootObj != null)
            {
                sceneObj.transform.SetParent(rootObj.transform, false);
            }

            foreach (var node in scene.Nodes)
            {
                var nodeObj = CreateNode(node.Value);
                nodeObj.transform.parent = sceneObj.transform;
            }

            return sceneObj;
        }

        private GameObject CreateNode(GLTFNode node)
        {
            var nodeObj = new GameObject(node.Name ?? "GLTFNode");

            // Set the transform properties from the GLTFNode's values.
            // Use the matrix first if set.
            if (node.Matrix != null)
            {
                var mat = new Matrix4x4();

                for (var i = 0; i < 16; i++)
                {
                    mat[i] = (float)node.Matrix[i];
                }

                nodeObj.transform.localPosition = mat.GetColumn(3);

                nodeObj.transform.localScale = new Vector3(
                    mat.GetColumn(0).magnitude,
                    mat.GetColumn(1).magnitude,
                    mat.GetColumn(2).magnitude
                );

                var w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
                var w4 = 4.0f * w;
                var x = (mat.m21 - mat.m12) / w4;
                var y = (mat.m02 - mat.m20) / w4;
                var z = (mat.m10 - mat.m01) / w4;

                x = float.IsNaN(x) ? 0 : x;
                y = float.IsNaN(y) ? 0 : y;
                z = float.IsNaN(z) ? 0 : z;

                nodeObj.transform.localRotation = new Quaternion(x, y, z, w);
            }
            // Otherwise fall back to the TRS properties.
            else
            {
                nodeObj.transform.localPosition = node.Translation;
                nodeObj.transform.localScale = node.Scale;
                nodeObj.transform.localRotation = node.Rotation;
            }

            // TODO: Add support for skin/morph targets
            if (node.Mesh != null)
            {
                var meshObj = FindOrCreateMeshObject(node.Mesh);
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

        private GameObject FindOrCreateMeshObject(GLTFMeshId meshId)
        {
            GameObject meshObj;

            if (_meshCache.TryGetValue(meshId.Value, out meshObj))
            {
                return GameObject.Instantiate(meshObj);
            }

            return CreateMeshObject(meshId.Value);
        }

        private GameObject CreateMeshObject(GLTFMesh mesh)
        {
            var meshName = mesh.Name ?? "GLTFMesh";
            var meshObj = new GameObject(meshName);

            // Flip the z scale to account for Unity's left handed coordinate system
            // vs GLTF's right handed system.
            var scale = meshObj.transform.localScale;
            scale.z *= -1;
            meshObj.transform.localScale = scale;

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

        private GLTFMeshPrimitiveAttributes BuildMeshAttributes(GLTFMeshPrimitive primitive)
        {
            var attributes = new GLTFMeshPrimitiveAttributes();

            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.POSITION))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.POSITION].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Vertices = accessor.AsVector3Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.NORMAL))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.NORMAL].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Normals = accessor.AsVector3Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(0)))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.TexCoord(0)].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Uv = accessor.AsVector2Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(1)))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.TexCoord(1)].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Uv2 = accessor.AsVector2Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(2)))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.TexCoord(2)].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Uv3 = accessor.AsVector2Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.TexCoord(3)))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.TexCoord(3)].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Uv4 = accessor.AsVector2Array(bufferData);
            }
            if (primitive.Attributes.ContainsKey(GLTFSemanticProperties.Color(0)))
            {
                var accessor = primitive.Attributes[GLTFSemanticProperties.Color(0)].Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Colors = accessor.AsColorArray(bufferData);
            }

            {
                var accessor = primitive.Indices.Value;
                var bufferData = _bufferCache[accessor.BufferView.Value.Buffer.Value];
                attributes.Triangles = accessor.AsIntArray(bufferData);
            }

            return CalculateAndSetTangents(attributes);
        }

        // Taken from: http://answers.unity3d.com/comments/190515/view.html
        // Official support for Mesh.RecalculateTangents should be coming in 5.6
        // https://feedback.unity3d.com/suggestions/recalculatetangents
        private GLTFMeshPrimitiveAttributes CalculateAndSetTangents(GLTFMeshPrimitiveAttributes attributes)
        {
            var triangleCount = attributes.Triangles.Length;
            var vertexCount = attributes.Vertices.Length;

            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            attributes.Tangents = new Vector4[vertexCount];

            for (long a = 0; a < triangleCount; a += 3)
            {
                long i1 = attributes.Triangles[a + 0];
                long i2 = attributes.Triangles[a + 1];
                long i3 = attributes.Triangles[a + 2];

                var v1 = attributes.Vertices[i1];
                var v2 = attributes.Vertices[i2];
                var v3 = attributes.Vertices[i3];

                var w1 = attributes.Uv[i1];
                var w2 = attributes.Uv[i2];
                var w3 = attributes.Uv[i3];

                var x1 = v2.x - v1.x;
                var x2 = v3.x - v1.x;
                var y1 = v2.y - v1.y;
                var y2 = v3.y - v1.y;
                var z1 = v2.z - v1.z;
                var z2 = v3.z - v1.z;

                var s1 = w2.x - w1.x;
                var s2 = w3.x - w1.x;
                var t1 = w2.y - w1.y;
                var t2 = w3.y - w1.y;

                var r = 1.0f / (s1 * t2 - s2 * t1);

                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }


            for (long a = 0; a < vertexCount; ++a)
            {
                var n = attributes.Normals[a];
                var t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);

                attributes.Tangents[a].x = t.x;
                attributes.Tangents[a].y = t.y;
                attributes.Tangents[a].z = t.z;

                attributes.Tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            return attributes;
        }

        private Material FindOrCreateMaterial(GLTFMaterial materialDef)
        {
            Material material;

            if (_materialCache.TryGetValue(materialDef, out material))
            {
                return material;
            }

            return CreateMaterial(materialDef);
        }

        private Material CreateMaterial(GLTFMaterial def)
        {
            var material = new Material(Shader.Find("GLTF/GLTFMetallicRoughness"));

            if (def.PbrMetallicRoughness != null)
            {
                var pbr = def.PbrMetallicRoughness;

                material.SetColor("_BaseColorFactor", pbr.BaseColorFactor);

                if (pbr.BaseColorTexture != null)
                {
                    var texture = pbr.BaseColorTexture.Index.Value;
                    material.SetTexture("_MainTex", _imageCache[texture.Source.Value]);
                    material.SetTextureScale("_MainTex", new Vector2(1, -1));
                }

                material.SetFloat("_MetallicFactor", (float)pbr.MetallicFactor);

                if (pbr.MetallicRoughnessTexture != null)
                {
                    var texture = pbr.MetallicRoughnessTexture.Index.Value;
                    material.SetTexture("_MetallicRoughnessMap", _imageCache[texture.Source.Value]);
                    material.SetTextureScale("_MetallicRoughnessMap", new Vector2(1, -1));
                }

                material.SetFloat("_RoughnessFactor", (float)pbr.RoughnessFactor);
            }

            if (def.NormalTexture != null)
            {
                var texture = def.NormalTexture.Index.Value;
                material.SetTexture("_NormalMap", _imageCache[texture.Source.Value]);
                material.SetTextureScale("_NormalMap", new Vector2(1, -1));
                material.SetFloat("_NormalScale", (float)def.NormalTexture.Scale);
            }

            if (def.OcclusionTexture != null)
            {
                var texture = def.OcclusionTexture.Index.Value;
                material.SetTexture("_OcclusionMap", _imageCache[texture.Source.Value]);
                material.SetTextureScale("_OcclusionMap", new Vector2(1, -1));
                material.SetFloat("_OcclusionStrength", (float)def.OcclusionTexture.Strength);
            }

            if (def.EmissiveTexture != null)
            {
                var texture = def.EmissiveTexture.Index.Value;
                material.SetTextureScale("_EmissiveMap", new Vector2(1, -1));
                material.SetTexture("_EmissiveMap", _imageCache[texture.Source.Value]);
            }

            material.SetColor("_EmissiveFactor", def.EmissiveFactor);

            return material;
        }

        private static string Base64StringInitializer = "data:application/octet-stream;base64,";

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
            byte[] bufferData;

            if (buffer.Uri == null)
            {
                bufferData = new byte[0];
            }
            else
            {
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
            }

            _bufferCache[buffer] = bufferData;
        }
    }
}
