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
    public class GltfSceneAudioEmitterExtension : IExtension
    {
        public const string SceneAudioExtensionName = "GOOG_audio_emitter";

        public List<int> emitters = new(); 

        public GltfSceneAudioEmitterExtension()
        {
        }

        /// <summary>
        /// Called when the data is written and serialized to file.
        /// </summary>
        public JProperty Serialize()
        {
           JObject jo = new JObject();

           JArray arr = new JArray();
 
            foreach (var emitter in emitters) {
                arr.Add(emitter);
            }

            jo.Add(new JProperty(nameof(emitters), arr));


            JProperty extension =
                new JProperty(GltfSceneAudioEmitterExtension.SceneAudioExtensionName, jo);
            return extension;
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public IExtension Clone(GLTFRoot root)
        {
            return new GltfSceneAudioEmitterExtension()
            {
                emitters = emitters
            };
        }
    }
}
