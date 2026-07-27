namespace Unity.VisualScripting
{
    /// <summary>
    /// Reads the implementation-specific runtime limits exposed by KHR_interactivity as the
    /// read-only limits/maxActive* pointers (spec §4.2.2 Implementation-Specific Runtime Limits).
    /// An implementation may report 2147483647 (int.MaxValue) when it has no explicit limit.
    /// </summary>
    [UnitCategory("glTF/Interactivity")]
    [UnitTitle("Get glTF Runtime Limits")]
    public class GltfRuntimeLimitsUnit : Unit
    {
        [DoNotSerialize]
        public ValueOutput maxActiveAnimations { get; private set; }

        [DoNotSerialize]
        public ValueOutput maxActiveDelays { get; private set; }

        [DoNotSerialize]
        public ValueOutput maxActivePropertyInterpolations { get; private set; }

        [DoNotSerialize]
        public ValueOutput maxActiveVariableInterpolations { get; private set; }

        protected override void Definition()
        {
            // Best-effort constants for in-editor play mode; the real values are provided by the
            // KHR_interactivity runtime of the viewer that presents the exported asset.
            maxActiveAnimations = ValueOutput<int>("maxActiveAnimations", _ => int.MaxValue);
            maxActiveDelays = ValueOutput<int>("maxActiveDelays", _ => int.MaxValue);
            maxActivePropertyInterpolations = ValueOutput<int>("maxActivePropertyInterpolations", _ => int.MaxValue);
            maxActiveVariableInterpolations = ValueOutput<int>("maxActiveVariableInterpolations", _ => int.MaxValue);
        }
    }
}
