using System;
using System.Collections.Generic;
using GLTF.Schema;

namespace GLTF.Utilities
{
    public static class GltfRootExtensions
    {
        public static int[] GetAllNodeIdsWithMaterialId(this GLTFRoot root, int id)
        {
            List<int> ids = new List<int>();
            
            if (root.Meshes == null)
                return Array.Empty<int>();            

            for (int iMeshes = 0; iMeshes < root.Meshes.Count; iMeshes++)
            {
                var mesh = root.Meshes[iMeshes];
                
                if (mesh == null || mesh.Primitives == null)
                    continue;

                for (int iPrimitives = 0; iPrimitives < mesh.Primitives.Count; iPrimitives++)
                {
                    if (mesh.Primitives[iPrimitives] != null && mesh.Primitives[iPrimitives].Material != null && mesh.Primitives[iPrimitives].Material.Id == id)
                    {
                        for (int iNodes = 0; iNodes < root.Nodes.Count; iNodes++)
                        {
                            if (root.Nodes[iNodes].Mesh != null &&root.Nodes[iNodes].Mesh.Id == iMeshes)
                            {
                                ids.Add(iNodes);
                            }
                        }
                        break;
                    }
                }
            }
            
            return ids.ToArray();
        }

        public static int[] GetAllNodeIdsWithCameraId(this GLTFRoot root, int id)
        {
            List<int> ids = new List<int>();

            if (root.Nodes == null)
                return Array.Empty<int>();
            
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                var node = root.Nodes[i];
                
                if (node != null && node.Camera != null && node.Camera.Id == id)
                    ids.Add(i);
            }

            return ids.ToArray();
        }        
        
    }
}