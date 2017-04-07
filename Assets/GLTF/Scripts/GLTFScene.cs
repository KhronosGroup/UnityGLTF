using System.Collections;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// The root nodes of a scene.
    /// </summary>
    public class GLTFScene
    {
        /// <summary>
        /// The indices of each root node.
        /// </summary>
        public GLTFNodeId[] nodes = { };

        public string name;

        /// <summary>
        /// Create the GameObject for the GLTFScene and set it as a child of the gltfRoot's GameObject.
        /// </summary>
        /// <param name="gltfRoot">The GLTFRoot object</param>
        public GameObject Create(GameObject gltfRoot)
        {
            return Create(gltfRoot, new GLTFConfig());
        }

        /// <summary>
        /// Create the GameObject for the GLTFScene and set it as a child of the gltfRoot's GameObject.
        /// </summary>
        /// <param name="gltfRoot">The GLTFRoot object</param>
        /// <param name="config">Config for GLTF scene creation.</param>
        public GameObject Create(GameObject gltfRoot, GLTFConfig config)
        {
            GameObject sceneObj = new GameObject(name ?? "GLTFScene");
            sceneObj.transform.SetParent(gltfRoot.transform, false);

            foreach (var node in nodes)
            {
                node.Value.Create(sceneObj, config); 
            }

            return sceneObj;
        }
    }
}
