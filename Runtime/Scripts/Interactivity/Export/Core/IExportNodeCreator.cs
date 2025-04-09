using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Export
{
    public interface INodeExporter
    {
        GltfInteractivityExportNode CreateNode(GltfInteractivityNodeSchema schema);
        InteractivityExportContext Context { get; }
        
    }
}