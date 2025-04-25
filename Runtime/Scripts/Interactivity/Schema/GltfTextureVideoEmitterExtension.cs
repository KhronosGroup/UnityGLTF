using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using Newtonsoft.Json.Linq;
    using UnityGLTF.Interactivity;
    using UnityGLTF.Plugins.Experimental;

    [Serializable]

	/// <summary>
	/// Video Emitter Extension class that is called to serialize the data for the Texture node
	/// </summary>
    public class GltfTextureVideoEmitterExtension : IExtension
    {
        public const string TextureVideoExtensionName = "GOOG_video";

        public GOOG_VideoData[] videos; 

        public GltfTextureVideoEmitterExtension()
        {
        }

        /// <summary>
        /// Called when the data is written and serialized to file.
        /// </summary>
        public JProperty Serialize()
        {
//            JObject jo = new JObject();
            JArray arr = new JArray();

            foreach (var vid in videos)
            {
                arr.Add(vid.SerializeObject());
            }
            JProperty extension =
                new JProperty(TextureVideoExtensionName, arr);
            return extension;
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public IExtension Clone(GLTFRoot root)
        {
            return new GltfTextureVideoEmitterExtension()
            {
                videos = videos
            };
        }
    }
}
