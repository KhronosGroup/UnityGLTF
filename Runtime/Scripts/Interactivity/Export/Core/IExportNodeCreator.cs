using System;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public interface INodeExporter
    {
        GltfInteractivityExportNode CreateNode<TSchema>() where TSchema : GltfInteractivityNodeSchema, new();
        GltfInteractivityExportNode CreateNode(Type schemaType);
        
        InteractivityExportContext Context { get; }
        
    }
}