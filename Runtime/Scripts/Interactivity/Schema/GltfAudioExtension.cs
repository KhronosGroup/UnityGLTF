using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    using System;
    using System.Linq;
    using GLTF.Schema;
    using Newtonsoft.Json.Linq;
    using UnityGLTF.Interactivity;

    [Serializable]

	/// <summary>
	/// Audio Extension class that is called to serialize the data
	/// </summary>
    public class GltfAudioExtension : IExtension
    {
        public const string AudioExtensionName = "GOOG_audio_emitter";

        public GltfInteractivityGraph[] graphs;
        public int graph = 0;

        public GltfAudioExtension()
        {
        }

        /// <summary>
        /// Called when the data is written and serialized to file.
        /// </summary>
        public JProperty Serialize()
        {
            JObject jo = new JObject
            {
                new JProperty("graphs",
                    new JArray(
                        from gr in graphs
                        select gr.SerializeObject())),
                new JProperty("graph", graph)
            };

            JProperty extension =
                new JProperty(GltfAudioExtension.AudioExtensionName, jo);
            return extension;
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public IExtension Clone(GLTFRoot root)
        {
            return new GltfAudioExtension()
            {
                graph = graph,
                graphs = graphs
            };
        }
    }
}
