using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public class MathSmoothStepSpec : MathThreeOperandsSpec<float, float2, float3, float4>
    {
        public MathSmoothStepSpec() : base("Computed interpolation coefficient.", "First edge.", "Second edge.") { }
    }
}
