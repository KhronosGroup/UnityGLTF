using System.Collections.Generic;
using Newtonsoft.Json;

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
    }
}
