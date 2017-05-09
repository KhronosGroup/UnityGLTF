using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace GLTF
{
    public class GLTFExporter
    {
        private Transform _rootTransform;
        private GLTFRoot _root;

        public bool ExportNames = true;

        public GLTFExporter(Transform rootTransform)
        {
            _rootTransform = rootTransform;
            _root = new GLTFRoot();
            _root.Asset = new GLTFAsset {
                Version = "2.0"
            };
            _root.Accessors = new List<GLTFAccessor>();
            _root.Scenes = new List<GLTFScene>();
            _root.Nodes = new List<GLTFNode>();
            _root.Materials = new List<GLTFMaterial>();
            _root.Meshes = new List<GLTFMesh>();
            _root.Scene = ExportScene(_rootTransform);
        }

        public string SerializeGLTF()
        {
            var stringWriter = new StringWriter();
            var writer = new JsonTextWriter(stringWriter);
            _root.Serialize(writer);
            return stringWriter.ToString();
        }

        private GLTFSceneId ExportScene(Transform sceneTransform)
        {
            var scene = new GLTFScene();

            if (ExportNames)
            {
                scene.Name = sceneTransform.name;
            }

            scene.Nodes = new List<GLTFNodeId>(1);
            scene.Nodes.Add(ExportNode(sceneTransform));
            
            _root.Scenes.Add(scene);

            return new GLTFSceneId {
                Id = _root.Scenes.Count - 1,
                Root = _root
            };
        }

        private GLTFNodeId ExportNode(Transform nodeTransform)
        {
            var node = new GLTFNode();

            if (ExportNames)
            {
                node.Name = nodeTransform.name;
            }

            node.SetUnityTransform(nodeTransform);

            var meshFilter = nodeTransform.GetComponent<MeshFilter>();
            var meshRenderer = nodeTransform.GetComponent<MeshRenderer>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                node.Mesh = ExportMesh(meshFilter.sharedMesh, meshRenderer.sharedMaterial);
            }

            var id = new GLTFNodeId {
                Id = _root.Nodes.Count,
                Root = _root
            };
            _root.Nodes.Add(node);

            var childCount = nodeTransform.childCount;

            if (childCount > 0)
            {
                node.Children = new List<GLTFNodeId>(childCount);
                for(var i = 0; i < childCount; i++)
                {
                    var childTransform = nodeTransform.GetChild(i);
                    node.Children.Add(ExportNode(childTransform));
                }
            }
            
            return id;
        }

        private GLTFMeshId ExportMesh(Mesh meshObj, Material materialObj)
        {
            var mesh = new GLTFMesh();

            if (ExportNames)
            {
                mesh.Name = meshObj.name;
            }

            var primitive = new GLTFMeshPrimitive();

            primitive.Attributes = new Dictionary<string, GLTFAccessorId>();
            
            var vertices = meshObj.vertices;
            primitive.Attributes.Add("POSITION", ExportAccessor(InvertZ(vertices)));

            var triangles = meshObj.triangles;
            primitive.Indices = ExportAccessor(FlipFaces(triangles));

            var normals = meshObj.normals;
            if (normals.Length != 0)
            {
                primitive.Attributes.Add("NORMAL", ExportAccessor(InvertZ(normals)));
            }

            var tangents = meshObj.tangents;
            if (tangents.Length != 0)
            {
                primitive.Attributes.Add("TANGENT", ExportAccessor(InvertW(tangents)));
            }

            var uv = meshObj.uv;
            if (uv.Length != 0)
            {
                primitive.Attributes.Add("TEXCOORD_0", ExportAccessor(InvertY(uv)));
            }

            var uv2 = meshObj.uv2;
            if (uv2.Length != 0)
            {
                primitive.Attributes.Add("TEXCOORD_1", ExportAccessor(InvertY(uv2)));
            }

            var colors = meshObj.colors;
            if (colors.Length != 0)
            {
                primitive.Attributes.Add("COLOR_0", ExportAccessor(colors));
            }

            primitive.Material = ExportMaterial(materialObj);

            mesh.Primitives = new List<GLTFMeshPrimitive> { primitive };

            var id = new GLTFMeshId {
                Id = _root.Meshes.Count,
                Root = _root
            };
            _root.Meshes.Add(mesh);

            return id;
        }

        private GLTFMaterialId ExportMaterial(Material materialObj)
        {
            var material = new GLTFMaterial();

            var id = new GLTFMaterialId {
                Id = _root.Materials.Count,
                Root = _root
            };
            _root.Materials.Add(material);

            return id;
        }

        private Vector2[] InvertY(Vector2[] arr)
        {
            var len = arr.Length;
            for(var i = 0; i < len; i++)
            {
                arr[i].y = -arr[i].y;
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

        private GLTFAccessorId ExportAccessor(int[] arr)
        {
            var count = arr.Length;
            
            if (count == 0)
            {
                throw new Exception("Accessors can not have a count of 0.");
            }

            var accessor = new GLTFAccessor();
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

            if (max < byte.MaxValue && min > byte.MinValue)
            {
                accessor.ComponentType = GLTFComponentType.UnsignedByte;
            }
            else if (max < sbyte.MaxValue && min > sbyte.MinValue)
            {
                accessor.ComponentType = GLTFComponentType.Byte;
            }
            else if (max < short.MaxValue && min > short.MinValue)
            {
                accessor.ComponentType = GLTFComponentType.Short;
            }
            else if (max < ushort.MaxValue && min > ushort.MinValue)
            {
                accessor.ComponentType = GLTFComponentType.UnsignedShort;
            }
            else if (min > uint.MinValue)
            {
                accessor.ComponentType = GLTFComponentType.UnsignedInt;
            }
            else
            {
                accessor.ComponentType = GLTFComponentType.Float;
            }

            accessor.Min = new List<double> { min };
            accessor.Max = new List<double> { max };

            var id = new GLTFAccessorId {
                Id = _root.Accessors.Count,
                Root = _root
            };
            _root.Accessors.Add(accessor);

            return id;
        }

        private GLTFAccessorId ExportAccessor(Vector2[] arr)
        {
            var count = arr.Length;
            
            if (count == 0)
            {
                throw new Exception("Accessors can not have a count of 0.");
            }

            var accessor = new GLTFAccessor();
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

            var id = new GLTFAccessorId {
                Id = _root.Accessors.Count,
                Root = _root
            };
            _root.Accessors.Add(accessor);

            return id;
        }

        private GLTFAccessorId ExportAccessor(Vector3[] arr)
        {
            var count = arr.Length;
            
            if (count == 0)
            {
                throw new Exception("Accessors can not have a count of 0.");
            }

            var accessor = new GLTFAccessor();
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

            var id = new GLTFAccessorId {
                Id = _root.Accessors.Count,
                Root = _root
            };
            _root.Accessors.Add(accessor);

            return id;
        }

        private GLTFAccessorId ExportAccessor(Vector4[] arr)
        {
            var count = arr.Length;
            
            if (count == 0)
            {
                throw new Exception("Accessors can not have a count of 0.");
            }

            var accessor = new GLTFAccessor();
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

            var id = new GLTFAccessorId {
                Id = _root.Accessors.Count,
                Root = _root
            };
            _root.Accessors.Add(accessor);

            return id;
        }

        private GLTFAccessorId ExportAccessor(Color[] arr)
        {
            var count = arr.Length;
            
            if (count == 0)
            {
                throw new Exception("Accessors can not have a count of 0.");
            }

            var accessor = new GLTFAccessor();
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

            var id = new GLTFAccessorId {
                Id = _root.Accessors.Count,
                Root = _root
            };
            _root.Accessors.Add(accessor);

            return id;
        }

        
    }
}