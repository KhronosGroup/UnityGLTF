using GLTF.Schema;

namespace UnityGLTF.Interactivity.Schema
{

    /// <summary>
    /// node/onSelect Node that fires an event when the node is selected.
    ///
    /// See https://github.com/petermart/glTF-InteractivityGraph-AuthoringTool/blob/483da2161aa3d9c01ef47be8cdb2bec2d1dd3a18/src/authoring/AuthoringNodeSpecs.ts#L419
    /// </summary>
    public class Event_OnSelectNode : GltfInteractivityNodeSchema
    {
        public override string Op { get; set; } = "event/onSelect";
        public override string Extension { get; protected set; } = KHR_node_selectability_Factory.EXTENSION_NAME;
        
        [ConfigDescription]
        public const string IdConfigNodeIndex = "nodeIndex";
        [ConfigDescription]
        public const string IdConfigStopPropagation = "stopPropagation";
        [FlowOutSocketDescription]
        public const string IdFlowOut = "out";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdValueSelectedNodeIndex = "selectedNodeIndex";
        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdValueSelectionRayOrigin = "selectionRayOrigin";
        [OutputSocketDescription(GltfTypes.Float3)]
        public const string IdValueLocalHitLocation = "selectionPoint";
        [OutputSocketDescription(GltfTypes.Int)]
        public const string IdValueControllerIndex = "controllerIndex";

    }
}
