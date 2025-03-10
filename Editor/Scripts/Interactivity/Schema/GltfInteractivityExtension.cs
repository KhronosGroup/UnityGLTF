using Editor.Schema;

namespace UnityGLTF.Interactivity
{
    using System;
    using System.Linq;
    using GLTF.Schema;
    using Newtonsoft.Json.Linq;
    
    
    [Serializable]
    internal class GltfInteractivityExtension : IExtension
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
