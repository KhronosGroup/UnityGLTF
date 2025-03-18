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
        public readonly List<GltfInteractivityNode> nodes = new List<GltfInteractivityNode>();

        public GltfInteractivityNode CreateNode(GltfInteractivityNodeSchema schema)
        {
            var newNode = new GltfInteractivityNode(schema);
            nodes.Add(newNode);
            newNode.Index = nodes.Count - 1;
            return newNode;
        }
        
        internal GltfInteractivityExportNodes(List<GltfInteractivityNode> nodes)
        {
            this.nodes = nodes;
        }
    }
}