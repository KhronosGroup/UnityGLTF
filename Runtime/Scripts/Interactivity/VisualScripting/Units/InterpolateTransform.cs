using System;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>
    /// Interpolates the world position, rotation and scale of a Transform to the ones of another Transform.
    /// The target values are resolved once when the interpolation starts.
    /// </summary>
    [UnitCategory("Transform")]
    [UnitTitle("Interpolate Transform")]
    public class InterpolateTransform : Unit, IGraphElementWithData, IGraphEventListener
    {
        public sealed class Data : IGraphElementData
        {
            public bool isListening = false;
            public bool running = false;

            public float time;
            public float duration;
            public Vector2 pointA;
            public Vector2 pointB;

            public Transform transform;

            public Vector3 startPosition;
            public Quaternion startRotation;
            public Vector3 startScale;

            public Vector3 endPosition;
            public Quaternion endRotation;
            public Vector3 endScale;

            public Vector3 lastPosition;
            public Quaternion lastRotation;
            public Vector3 lastScale;

            public Delegate update;
        }

        [Serialize, Inspectable, UnitHeaderInspectable("Position")]
        public bool position { get; set; } = true;

        [Serialize, Inspectable, UnitHeaderInspectable("Rotation")]
        public bool rotation { get; set; } = true;

        [Serialize, Inspectable, UnitHeaderInspectable("Scale")]
        public bool scale { get; set; } = true;

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput assign { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        [NullMeansSelf]
        public ValueInput target { get; private set; }

        [DoNotSerialize]
        [PortLabel("Target Value")]
        public ValueInput targetValue { get; private set; }

        [DoNotSerialize]
        [PortLabel("Duration")]
        public ValueInput duration { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point A")]
        public ValueInput pointA { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point B")]
        public ValueInput pointB { get; private set; }

        [DoNotSerialize]
        public ControlOutput assigned { get; private set; }

        [DoNotSerialize]
        public ControlOutput done { get; private set; }

        protected override void Definition()
        {
            assign = ControlInput(nameof(assign), Assign);
            assigned = ControlOutput(nameof(assigned));
            done = ControlOutput(nameof(done));
            Succession(assign, assigned);
            Succession(assign, done);

            target = ValueInput<Transform>(nameof(target), null).NullMeansSelf();
            Requirement(target, assign);

            targetValue = ValueInput<Transform>(nameof(targetValue), null);
            Requirement(targetValue, assign);

            duration = ValueInput(typeof(float), nameof(duration));
            duration.SetDefaultValue(1f);
            Requirement(duration, assign);

            pointA = ValueInput(typeof(Vector2), nameof(pointA));
            pointA.SetDefaultValue(new Vector2(1f, 1f));
            Requirement(pointA, assign);

            pointB = ValueInput(typeof(Vector2), nameof(pointB));
            pointB.SetDefaultValue(new Vector2(1f, 1f));
            Requirement(pointB, assign);
        }

        private ControlOutput Assign(Flow flow)
        {
            StartNewInterpolation(flow);
            return assigned;
        }

        public IGraphElementData CreateData()
        {
            return new Data();
        }

        public void StartListening(GraphStack stack)
        {
            var data = stack.GetElementData<Data>(this);

            if (data.isListening)
            {
                return;
            }

            var reference = stack.ToReference();
            var hook = new EventHook(EventHooks.Update, stack.machine);
            Action<EmptyEventArgs> update = args => TriggerUpdate(reference);
            EventBus.Register(hook, update);
            data.update = update;
            data.isListening = true;
        }

        public void StopListening(GraphStack stack)
        {
            var data = stack.GetElementData<Data>(this);

            if (!data.isListening)
            {
                return;
            }

            var hook = new EventHook(EventHooks.Update, stack.machine);
            EventBus.Unregister(hook, data.update);

            stack.ClearReference();

            data.update = null;
            data.isListening = false;
        }

        public bool IsListening(GraphPointer pointer)
        {
            return pointer.GetElementData<Data>(this).isListening;
        }

        private void TriggerUpdate(GraphReference reference)
        {
            using (var flow = Flow.New(reference))
            {
                Update(flow);
            }
        }

        private void StartNewInterpolation(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);

            var transform = flow.GetValue<Transform>(target);
            var value = flow.GetValue<Transform>(targetValue);
            if (!transform || !value)
            {
                data.running = false;
                return;
            }

            data.running = true;
            data.transform = transform;
            data.duration = flow.GetValue<float>(duration);
            data.pointA = flow.GetValue<Vector2>(pointA);
            data.pointB = flow.GetValue<Vector2>(pointB);
            data.time = 0f;

            data.startPosition = transform.localPosition;
            data.startRotation = transform.localRotation;
            data.startScale = transform.localScale;

            // Like the glTF export: the world values of the target value are converted once to local values
            var parent = transform.parent;
            data.endPosition = parent ? parent.InverseTransformPoint(value.position) : value.position;
            data.endRotation = parent ? Quaternion.Inverse(parent.rotation) * value.rotation : value.rotation;
            var parentScale = parent ? parent.lossyScale : Vector3.one;
            var worldScale = value.lossyScale;
            data.endScale = new Vector3(
                parentScale.x != 0f ? worldScale.x / parentScale.x : 0f,
                parentScale.y != 0f ? worldScale.y / parentScale.y : 0f,
                parentScale.z != 0f ? worldScale.z / parentScale.z : 0f);

            data.lastPosition = data.startPosition;
            data.lastRotation = data.startRotation;
            data.lastScale = data.startScale;
        }

        private bool WasChangedExternally(Data data)
        {
            var t = data.transform;
            return (position && !InterpolateHelper.AreValuesEqual(data.lastPosition, t.localPosition)) ||
                   (rotation && !InterpolateHelper.AreValuesEqual(data.lastRotation, t.localRotation)) ||
                   (scale && !InterpolateHelper.AreValuesEqual(data.lastScale, t.localScale));
        }

        private void Apply(Data data, float easedT)
        {
            var t = data.transform;
            if (position)
                t.localPosition = data.lastPosition = Vector3.Lerp(data.startPosition, data.endPosition, easedT);
            if (rotation)
                t.localRotation = data.lastRotation = Quaternion.Slerp(data.startRotation, data.endRotation, easedT);
            if (scale)
                t.localScale = data.lastScale = Vector3.Lerp(data.startScale, data.endScale, easedT);
        }

        public void Update(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);

            if (!data.running)
            {
                return;
            }

            // Stop the interpolation if the transform was destroyed or changed externally
            if (!data.transform || WasChangedExternally(data))
            {
                data.running = false;
                return;
            }

            data.time += Time.deltaTime;

            var finished = data.duration <= 0f || data.time >= data.duration;
            var t = finished ? 1f : InterpolateHelper.EvaluateEasing(data.pointA, data.pointB, data.time / data.duration);
            Apply(data, t);

            if (finished)
            {
                data.running = false;

                var stack = flow.PreserveStack();
                flow.RestoreStack(stack);
                flow.Invoke(done);
                flow.DisposePreservedStack(stack);
            }
        }
    }
}
