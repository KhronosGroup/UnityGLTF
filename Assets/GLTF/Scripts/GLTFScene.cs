using System.Collections.Generic;
using GLTF.JsonExtensions;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// The root nodes of a scene.
    /// </summary>
    [System.Serializable]
    public class GLTFScene : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The indices of each root node.
        /// </summary>
        public List<GLTFNodeId> nodes;

        public static GLTFScene Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var scene = new GLTFScene();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "nodes":
                        scene.nodes = GLTFNodeId.ReadList(root, reader);
                        break;
                    case "name":
                        scene.name = reader.ReadAsString();
                        break;
                    case "extensions":
                    case "extras":
                    default:
                        reader.Read();
                        break;
                }
            }

            return scene;
        }

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
