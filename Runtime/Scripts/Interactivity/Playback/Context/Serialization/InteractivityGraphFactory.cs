using GLTF.Schema;
using Newtonsoft.Json.Linq;

namespace UnityGLTF.Interactivity.Playback
{
    public class InteractivityGraphFactory : ExtensionFactory
    {
        public InteractivityGraphFactory()
        {
            ExtensionName = InteractivityGraphExtension.EXTENSION_NAME;
        }

        public override IExtension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            if (extensionToken == null)
                return null;
            
            var graph = new InteractivityGraphExtension();
            graph.Deserialize(extensionToken);
            return graph;
        }
    }
}