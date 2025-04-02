using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting
{
    public interface IInteractivityExport
    {
        void OnInteractivityExport(VisualScriptingExportContext context, GltfInteractivityExportNodes nodes);
    }

    public class GltfInteractivityExportNodes
    {
        public readonly List<GltfInteractivityExportNode> nodes = new List<GltfInteractivityExportNode>();

        public GltfInteractivityExportNode CreateNode(GltfInteractivityNodeSchema schema)
        {
            var newNode = new GltfInteractivityExportNode(schema);
            nodes.Add(newNode);
            newNode.Index = nodes.Count - 1;
            return newNode;
        }
        
        internal GltfInteractivityExportNodes(List<GltfInteractivityExportNode> nodes)
        {
            this.nodes = nodes;
        }
    }
}