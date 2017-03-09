using Newtonsoft.Json;
using System.Collections;

namespace GLTF
{
    /// <summary>
    /// The root object for a glTF asset.
    /// </summary>
    public class GLTFRoot
    {   
        /// <summary>
        /// Names of glTF extensions used somewhere in this asset.
        /// </summary>
        public string[] extensionsUsed;

        /// <summary>
        /// Names of glTF extensions required to properly load this asset.
        /// </summary>
        public string[] extensionsRequired;

        /// <summary>
        /// An array of accessors. An accessor is a typed view into a bufferView.
        /// </summary>
        public GLTFAccessor[] accessors;

        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        public GLTFAnimation[] animations;

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public GLTFAsset asset;

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        public GLTFBuffer[] buffers;

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        public GLTFBufferView[] bufferViews;

        /// <summary>
        /// An array of cameras. A camera defines a projection matrix.
        /// </summary>
        public GLTFCamera[] cameras;

        /// <summary>
        /// An array of images. An image defines data used to create a texture.
        /// </summary>
        public GLTFImage[] images;

        /// <summary>
        /// An array of materials. A material defines the appearance of a primitive.
        /// </summary>
        public GLTFMaterial[] materials;

        /// <summary>
        /// An array of meshes. A mesh is a set of primitives to be rendered.
        /// </summary>
        public GLTFMesh[] meshes;

        /// <summary>
        /// An array of nodes.
        /// </summary>
        public GLTFNode[] nodes;

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        public GLTFSampler[] samplers;

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        public GLTFSceneId scene;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        public GLTFScene[] scenes;

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        public GLTFSkin[] skins;

        /// <summary>
        /// An array of textures.
        /// </summary>
        public GLTFTexture[] textures;

        /// <summary>
        /// Load all the referenced data from the URIs in gltf.buffers and gltf.images
        /// </summary>
        /// <returns>IEnumerator to yield return inside a Unity lifecycle method such as Start.</returns>
        public IEnumerator LoadAllScenes()
        {
            foreach(var buffer in buffers)
            {
                yield return buffer.Load();
            }

            foreach (var image in images)
            {
                yield return image.Load();
            }

            // Return null in case there are no buffers or images.
            yield return null;
        }
    }

    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    public class GLTFAsset {
        /// <summary>
        /// A copyright message suitable for display to credit the content creator.
        /// </summary>
        public string copyright;

        /// <summary>
        /// Tool that generated this glTF model. Useful for debugging.
        /// </summary>
        public string generator;

        /// <summary>
        /// The glTF version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string version;
    }
}
