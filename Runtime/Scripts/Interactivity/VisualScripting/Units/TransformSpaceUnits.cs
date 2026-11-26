using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Converts a value between world space and the local space of <see cref="target"/>.
    /// "Local" is the space of the target itself (like <see cref="Transform.TransformPoint(Vector3)"/>),
    /// to get a value for a child's localPosition/localRotation, pass the child's parent as target.
    /// </summary>
    [TypeIcon(typeof(Transform))]
    public abstract class TransformSpaceUnit<T> : Unit
    {
        [DoNotSerialize, NullMeansSelf, PortLabelHidden]
        public ValueInput target { get; private set; }

        [DoNotSerialize]
        public ValueInput input { get; private set; }

        [DoNotSerialize]
        public ValueOutput output { get; private set; }

        protected abstract string inputName { get; }
        protected abstract string outputName { get; }

        protected abstract T Convert(Transform transform, T value);

        protected override void Definition()
        {
            target = ValueInput<Transform>(nameof(target), null).NullMeansSelf();
            // Visual Scripting has no inline default values for some types (e.g. Quaternion), those inputs must be connected
            input = Unity.VisualScripting.ValueInput.SupportsDefaultValue(typeof(T))
                ? ValueInput(inputName, default(T))
                : ValueInput<T>(inputName);
            output = ValueOutput<T>(outputName, flow => Convert(flow.GetValue<Transform>(target), flow.GetValue<T>(input)));

            Requirement(target, output);
            Requirement(input, output);
        }
    }

    [UnitCategory("Transform")]
    [UnitTitle("World To Local Position")]
    public class WorldToLocalPosition : TransformSpaceUnit<Vector3>
    {
        protected override string inputName => "worldPosition";
        protected override string outputName => "localPosition";

        protected override Vector3 Convert(Transform transform, Vector3 value) => transform.InverseTransformPoint(value);
    }

    [UnitCategory("Transform")]
    [UnitTitle("Local To World Position")]
    public class LocalToWorldPosition : TransformSpaceUnit<Vector3>
    {
        protected override string inputName => "localPosition";
        protected override string outputName => "worldPosition";

        protected override Vector3 Convert(Transform transform, Vector3 value) => transform.TransformPoint(value);
    }

    [UnitCategory("Transform")]
    [UnitTitle("World To Local Rotation")]
    public class WorldToLocalRotation : TransformSpaceUnit<Quaternion>
    {
        protected override string inputName => "worldRotation";
        protected override string outputName => "localRotation";

        protected override Quaternion Convert(Transform transform, Quaternion value) => Quaternion.Inverse(transform.rotation) * value;
    }

    [UnitCategory("Transform")]
    [UnitTitle("Local To World Rotation")]
    public class LocalToWorldRotation : TransformSpaceUnit<Quaternion>
    {
        protected override string inputName => "localRotation";
        protected override string outputName => "worldRotation";

        protected override Quaternion Convert(Transform transform, Quaternion value) => transform.rotation * value;
    }
}
