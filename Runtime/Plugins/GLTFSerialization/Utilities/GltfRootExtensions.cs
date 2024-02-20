using System.Collections;
using System.Collections.Generic;
using GLTF.Schema;

namespace GLTF.Utilities
{
    public static class GltfRootExtensions
    {
        public static int[] GetAllNodeIdsWithMaterialId(this GLTFRoot root, int id)
        {
            List<int> ids = new List<int>();

            for (int iMeshes = 0; iMeshes < root.Meshes.Count; iMeshes++)
            {
                if (root.Meshes[iMeshes].Primitives == null)
                    continue;
                
                for (int iPrimitives = 0; iPrimitives < root.Meshes[iMeshes].Primitives.Count; iPrimitives++)
                {
                    if (root.Meshes[iMeshes].Primitives[iPrimitives].Material != null && root.Meshes[iMeshes].Primitives[iPrimitives].Material.Id == id)
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

            for (int i = 0; i < root.Nodes.Count; i++)
            {
                if (root.Nodes[i].Camera != null && root.Nodes[i].Camera.Id == id)
                    ids.Add(i);
            }

            return ids.ToArray();
        }        
        
    }
}