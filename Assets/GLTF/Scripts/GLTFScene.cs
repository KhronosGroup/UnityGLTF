using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GLTF
{
    /// <summary>
    /// The root nodes of a scene.
    /// </summary>
    public class GLTFScene : GLTFChildOfRootProperty
    {
        /// <summary>
        /// The indices of each root node.
        /// </summary>
        public List<GLTFNodeId> Nodes;

        public static GLTFScene Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var scene = new GLTFScene();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "nodes":
                        scene.Nodes = GLTFNodeId.ReadList(root, reader);
                        break;
					default:
						scene.DefaultPropertyDeserializer(root, reader);
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
            var sceneObj = new GameObject(Name ?? "GLTFScene");
            sceneObj.transform.SetParent(gltfRoot.transform, false);

            foreach (var node in Nodes)
            {
                node.Value.Create(sceneObj, config); 
            }

            return sceneObj;
        }
    }
}
