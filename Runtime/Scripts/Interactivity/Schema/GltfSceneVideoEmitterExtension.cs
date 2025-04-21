using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GLTF.Schema;
    using Newtonsoft.Json.Linq;
    using UnityGLTF.Interactivity;

    [Serializable]

	/// <summary>
	/// Audio Emitter Extension class that is called to serialize the data for the scenes node
	/// </summary>
    public class GltfSceneVideoEmitterExtension : IExtension
    {
        public const string SceneVideoExtensionName = "GOOG_video";

        public List<int> videos = new(); 

        public GltfSceneVideoEmitterExtension()
        {
        }

        /// <summary>
        /// Called when the data is written and serialized to file.
        /// </summary>
        public JProperty Serialize()
        {
           JObject jo = new JObject();

           JArray arr = new JArray();
 
            foreach (var vid in videos) {
                arr.Add(vid);
            }

            jo.Add(new JProperty(nameof(videos), arr));


            JProperty extension =
                new JProperty(GltfSceneVideoEmitterExtension.SceneVideoExtensionName, jo);
            return extension;
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public IExtension Clone(GLTFRoot root)
        {
            return new GltfSceneVideoEmitterExtension()
            {
                videos = videos
            };
        }
    }
}
