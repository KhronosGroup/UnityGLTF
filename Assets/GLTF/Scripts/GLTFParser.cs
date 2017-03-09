using Newtonsoft.Json;

namespace GLTF {
    public class GLTFParser
    {
        /// <summary>
        /// Parse the gltf data as a JSON string into a GLTFRoot object.
        /// </summary>
        /// <param name="gltfPath">The path/url to the GLTF file.</param>
        /// <param name="gltf">The gltf data as a JSON string.</param>
        /// <returns>The parsed GLTFRoot object.</returns>
        public static GLTFRoot Parse (string gltfPath, string gltf)
        {
            
            GLTFRootRef rootRef = new GLTFRootRef();

            // Register all of the JSON converters for the various ID types.
            GLTFRoot root = JsonConvert.DeserializeObject<GLTFRoot>(
                gltf,
                new GLTFUriConverter(gltfPath),
                new GLTFAccessorIdConverter(rootRef),
                new GLTFBufferIdConverter(rootRef),
                new GLTFBufferViewIdConverter(rootRef),
                new GLTFCameraIdConverter(rootRef),
                new GLTFImageIdConverter(rootRef),
                new GLTFMaterialIdConverter(rootRef),
                new GLTFMeshIdConverter(rootRef),
                new GLTFNodeIdConverter(rootRef),
                new GLTFSamplerIdConverter(rootRef),
                new GLTFSceneIdConverter(rootRef),
                new GLTFSkinIdConverter(rootRef),
                new GLTFTextureIdConverter(rootRef)
            );

            rootRef.root = root;

            return root;
        }
    }
}