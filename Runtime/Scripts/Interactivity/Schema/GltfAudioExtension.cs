using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    using System;
    using System.Linq;
    using GLTF.Schema;
    using Newtonsoft.Json.Linq;
    using UnityGLTF.Interactivity;

    [Serializable]
    public class GltfAudioExtension : IExtension
    {
        public const string AudioExtensionName = "KHR_audio_emitter";

        public GltfInteractivityGraph[] graphs;
        public int graph = 0;

        public GltfAudioExtension()
        {
        }

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
