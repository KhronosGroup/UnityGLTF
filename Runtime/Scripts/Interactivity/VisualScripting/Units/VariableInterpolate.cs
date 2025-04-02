using System;
using UnityEngine;

namespace Unity.VisualScripting
{
    [UnitCategory("Variables")]
    [UnitTitle("Variable Interpolate")]
    public class VariableInterpolate : Unit, IUnifiedVariableUnit, IGraphElementWithData, IGraphEventListener
    {
        public sealed class Data : IGraphElementData
        {
            public bool isListening = false;
            public bool running = false;

            public float time;

            public object lastSetValue;
            public object startValue;

            public object endValue;

            public float duration;

            public Vector2 pointA;
            public Vector2 pointB;
            
            public Delegate update;
        }
        
        /// <summary>
        /// The kind of variable.
        /// </summary>
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VariableKind kind { get; set; }

        /// <summary>
        /// The name of the variable.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueInput name { get; private set; }

        /// <summary>
        /// The source of the variable.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        [NullMeansSelf]
        public ValueInput @object { get; private set; }
        
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
            name = ValueInput(nameof(name), string.Empty);

            if (kind == VariableKind.Object)
            {
                @object = ValueInput<GameObject>(nameof(@object), null).NullMeansSelf();
            }
            
            assign = ControlInput(nameof(assign), Assign);

            assigned = ControlOutput(nameof(assigned));
            done = ControlOutput(nameof(done));
            Succession(assign, assigned);
            Succession(assign, done);
            
            targetValue = ValueInput<object>(nameof(targetValue)).AllowsNull();
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
            data.pointA = flow.GetValue<Vector2>(pointA); 
            data.pointB = flow.GetValue<Vector2>(pointB);
            data.time = 0f;
            data.lastSetValue = GetValue(flow);
            data.startValue = data.lastSetValue;
            data.endValue = flow.GetValue<object>(targetValue);
        }

        private VariableDeclarations GetDeclarations(Flow flow)
        {
            switch (kind)
            {
                case VariableKind.Flow:
                    return flow.variables;
                case VariableKind.Graph:
                    return Variables.Graph(flow.stack);
                case VariableKind.Object:
                    return Variables.Object(flow.GetValue<GameObject>(@object));
                case VariableKind.Scene:
                    return Variables.Scene(flow.stack.scene);
                case VariableKind.Application:
                    return Variables.Application;
                case VariableKind.Saved:
                    return Variables.Saved;
                default:
                    throw new UnexpectedEnumValueException<VariableKind>(kind);
            }
        }
        
        private object GetValue(Flow flow)
        {
            var name = flow.GetValue<string>(this.name);
            var variables = GetDeclarations(flow);
            return variables.Get(name);
        }

        protected bool SetValue(Flow flow, Data data, object newValue)
        {
            var name = flow.GetValue<string>(this.name);
            var currentSetValue = GetValue(flow);
            if (!InterpolateHelper.AreValuesEqual(data.lastSetValue, currentSetValue))
                return false;

            var variables = GetDeclarations(flow);
            variables.Set(name, newValue);
            return true;
        }

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
            if (!SetValue(flow, data, currentValue))
            {
                // Stop the interpolation if the value was changed externally
                data.running = false;
            }
            else
            {
                data.lastSetValue = currentValue;
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