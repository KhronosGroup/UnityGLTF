using System;
using UnityEngine;

namespace Unity.VisualScripting
{
    public abstract class AbstractMaterialInterpolate<TValueType> : Unit, IGraphElementWithData, IGraphEventListener
    {
        public sealed class Data : IGraphElementData
        {
            public bool isListening = false;
            public bool running = false;

            public float time;

            public TValueType lastSetValue;
            public TValueType startValue;

            public TValueType endValue;

            public float duration;

            public string valueName;

            public Material material;
            public Vector2 pointA;
            public Vector2 pointB;
            
            public Delegate update;
        }
        
        protected virtual TValueType defaultValue { get; }
        protected virtual string defaultValueName { get; }
        
        [DoNotSerialize]
        public ValueInput target { get; private set; }
        
        [DoNotSerialize]
        public ValueInput valueName { get; private set; }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput assign { get; private set; }

        [DoNotSerialize]
        [PortLabel("Target Value")]
        public ValueInput targetValue { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point A")]
        public ValueInput pointA { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point B")]
        public ValueInput pointB { get; private set; }
        
        [DoNotSerialize]
        [PortLabel("Duration")]
        public ValueInput duration { get; private set; }
        
        [DoNotSerialize]
        public ControlOutput assigned { get; private set; }

        [DoNotSerialize]
        public ControlOutput done { get; private set; }
        
        protected override void Definition()
        {
            target = ValueInput(typeof(Material), nameof(target));
            target.SetDefaultValue(typeof(Material).PseudoDefault());
            
            assign = ControlInput(nameof(assign), Assign);
            Requirement(target, assign);

            assigned = ControlOutput(nameof(assigned));
            done = ControlOutput(nameof(done));
            Succession(assign, assigned);
            Succession(assign, done);

            valueName = ValueInput(typeof(string), nameof(valueName));
            valueName.SetDefaultValue(defaultValueName);
            Requirement(valueName, assign);

            targetValue = ValueInput(typeof(TValueType), nameof(targetValue));
            targetValue.SetDefaultValue(defaultValue);
            Requirement(targetValue, assign);
            
            duration = ValueInput(typeof(float), nameof(duration));
            duration.SetDefaultValue(1f);
            Requirement(duration, assign);
            
            pointA = ValueInput(typeof(Vector2), nameof(pointA));
            pointA.SetDefaultValue(new Vector2(1f,1f));
            Requirement(pointA, assign);

            pointB = ValueInput(typeof(Vector2), nameof(pointB));
            pointB.SetDefaultValue(new Vector2(1f,1f));
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

        protected virtual void StartNewInterpolation(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);
            data.running = true;
            
            data.duration = flow.GetValue<float>(this.duration);
            data.material = flow.GetValue<Material>(target);
            data.valueName = flow.GetValue<string>(valueName);
            data.pointA = flow.GetValue<Vector2>(pointA); 
            data.pointB = flow.GetValue<Vector2>(pointB);
            data.time = 0f;
        }
        
        protected abstract bool SetValue(Flow flow, Data data, TValueType newValue);

        public void Update(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);

            if (!data.running)
            {
                return;
            }

            data.time += Time.deltaTime;
            
            var stack = flow.PreserveStack();
            var t = data.time / data.duration;
            var currentValue = InterpolateHelper.BezierInterpolate(data.pointA, data.pointB, data.startValue, data.endValue, t);
            if (!SetValue(flow, data, (TValueType)currentValue))
            {
                // Stop the interpolation if the value was changed externally
                data.running = false;
            }
            else
            {
                data.lastSetValue = (TValueType)currentValue;
            }
       
            if (!data.running || data.time >= data.duration)
            {
                bool callDone = data.running;
                
                if (data.running)
                    SetValue(flow, data, data.endValue);
                
                data.running = false;

                flow.RestoreStack(stack);

                if (callDone)
                    flow.Invoke(done);
            }

            flow.DisposePreservedStack(stack);
        }
    }
}