using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    public class Event_OnHoverOutNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/onHoverOut";
        public override string Extension { get; protected set; } = KHR_node_hoverability_Factory.EXTENSION_NAME;

        [ConfigDescription]
        public const string IdConfigNodeIndex = "nodeIndex";
        [ConfigDescription]
        public const string IdConfigStopPropagation = "stopPropagation";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOutHoverNodeIndex = "hoverNodeIndex";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdOutControllerIndex = "controllerIndex";
    }
}