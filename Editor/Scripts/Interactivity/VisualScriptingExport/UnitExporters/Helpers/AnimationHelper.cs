using UnityGLTF.Interactivity.Export;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.VisualScripting.Export
{
    public static class AnimationHelper
    {
        public static ValueOutRef GetAnimationLength(UnitExporter unitExporter, int animationId)
        {
            var pointerLength = unitExporter.CreateNode<Pointer_GetNode>();
            PointersHelper.SetupPointerTemplateAndTargetInput(pointerLength, "index", "/animations/{index}/extensions/KHR_interactivity/maxTime", GltfTypes.Float);
            pointerLength.ValueIn("index").SetValue(animationId);
            return pointerLength.FirstValueOut();
        }
        
    }
}