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

        public GltfInteractivityExportNode AddLog(LogHelper.LogLevel level, string messageTemplate)
        {
            string messagePrefix = "";
            switch (level)
            {
                case LogHelper.LogLevel.Warning:
                    messagePrefix = "Warning: ";
                    break;
                case LogHelper.LogLevel.Error:
                    messagePrefix = "Error: ";
                    break;
            }
            
            var gltf_Node = CreateNode(new Debug_LogNode());
            gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + messageTemplate;
            return gltf_Node;
        }
        
        internal GltfInteractivityExportNodes(List<GltfInteractivityExportNode> nodes)
        {
            this.nodes = nodes;
        }
    }
}