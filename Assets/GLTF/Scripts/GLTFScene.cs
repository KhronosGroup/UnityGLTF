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
        /// <param name="parent">The gltfRoot's GameObject</param>
        public GameObject Create(GameObject gltfRoot)
        {
            GameObject sceneObj = new GameObject(name ?? "GLTFScene");
            sceneObj.transform.SetParent(gltfRoot.transform, false);

            foreach (var node in nodes)
            {
                GameObject nodeObj = node.Value.AsGameObject();
                nodeObj.transform.SetParent(sceneObj.transform, false);
            }

            return sceneObj;
        }
    }
}
