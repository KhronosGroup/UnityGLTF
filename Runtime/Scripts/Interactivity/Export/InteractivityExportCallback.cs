using System.Collections.Generic;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public interface IInteractivityExport
    {
        void OnInteractivityExport(InteractivityExportContext context, GltfInteractivityExportNodes nodes);
    }
    
    public class GltfInteractivityExportNodes : INodeExporter
    {
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
        
        public readonly List<GltfInteractivityExportNode> nodes = new List<GltfInteractivityExportNode>();
        public InteractivityExportContext Context { get; private set; }
        public GltfInteractivityExportNode CreateNode(GltfInteractivityNodeSchema schema)
        {
            var newNode = new GltfInteractivityExportNode(schema);
            nodes.Add(newNode);
            newNode.Index = nodes.Count - 1;
            return newNode;
        }

        public GltfInteractivityExportNode AddLog(LogLevel level, string messageTemplate)
        {
            string messagePrefix = "";
            switch (level)
            {
                case LogLevel.Warning:
                    messagePrefix = "Warning: ";
                    break;
                case LogLevel.Error:
                    messagePrefix = "Error: ";
                    break;
            }
            
            var gltf_Node = CreateNode(new Debug_LogNode());
            gltf_Node.Configuration[Debug_LogNode.IdConfigMessage].Value = messagePrefix + messageTemplate;
            return gltf_Node;
        }
        
        internal GltfInteractivityExportNodes(InteractivityExportContext context)
        {
            this.nodes = context.Nodes;
            this.Context = context;
        }
    }
}