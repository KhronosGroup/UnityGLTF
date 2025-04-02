using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    
    [Serializable]
    public class GltfInteractivityExtension : IExtension
    {
        public const string ExtensionName = "KHR_interactivity";

        public GltfInteractivityGraph[] graphs;
        public int graph = 0;
        
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
                new JProperty(GltfInteractivityExtension.ExtensionName, jo);
            return extension;
        }
        
        public IExtension Clone(GLTFRoot root)
        {
            return new GltfInteractivityExtension()
            {
                graph = graph,
                graphs = graphs
            };
        }
    }
}
