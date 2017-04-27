using System.Collections.Generic;
using Newtonsoft.Json;
using GLTF.JsonExtensions;

namespace GLTF
{
    /// <summary>
    /// A set of primitives to be rendered. A node can contain one or more meshes.
    /// A node's transform places the mesh in the scene.
    /// </summary>
    public class GLTFMesh : GLTFChildOfRootProperty
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered with
        /// a material.
        /// <minItems>1</minItems>
        /// </summary>
        public List<GLTFMeshPrimitive> Primitives;

        /// <summary>
        /// Array of weights to be applied to the Morph Targets.
        /// <minItems>0</minItems>
        /// </summary>
        public List<double> Weights;

        public static GLTFMesh Deserialize(GLTFRoot root, JsonTextReader reader)
        {
            var mesh = new GLTFMesh();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var curProp = reader.Value.ToString();

                switch (curProp)
                {
                    case "primitives":
                        mesh.Primitives = reader.ReadList(() => GLTFMeshPrimitive.Deserialize(root, reader));
                        break;
                    case "weights":
                        mesh.Weights = reader.ReadDoubleList();
                        break;
					default:
						mesh.DefaultPropertyDeserializer(root, reader);
						break;
				}
            }

            return mesh;
        }
    }
}
