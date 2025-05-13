using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public interface IInteractivityExport
    {
        void OnInteractivityExport(GltfInteractivityExportNodes export);
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
        
        public GltfInteractivityExportNode CreateNode(Type schemaType)
        {
            var newNode = new GltfInteractivityExportNode(GltfInteractivityNodeSchema.GetSchema(schemaType));
            nodes.Add(newNode);
            newNode.Index = nodes.Count - 1;
            return newNode;
        }
        
        public GltfInteractivityExportNode CreateNode<TSchema>() where TSchema : GltfInteractivityNodeSchema, new()
        {
            var newNode = new GltfInteractivityExportNode<TSchema>();
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
            
            var gltf_Node = CreateNode<Debug_LogNode>();
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