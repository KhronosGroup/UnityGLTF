using System;
using UnityEngine;

//namespace UnityGLTF.Interactivity.Units
namespace Unity.VisualScripting
{
    /// <summary>
    /// Interpolates the value of a field or property over time via reflection.
    /// </summary>
    [SpecialUnit]
    public sealed class InterpolateMember : MemberUnit, IGraphElementWithData, IGraphEventListener
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

        public InterpolateMember() : base() { }

        public InterpolateMember(Member member) : base(member) { }

        
        [DoNotSerialize]
        [Unity.VisualScripting.MemberFilter(Fields = true, Properties = true, ReadOnly = false)]
        public Member setter
        {
            get => member;
            set => member = value;
        }

        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput assign { get; private set; }

        [DoNotSerialize]
        [PortLabel("Target Value")]
        public ValueInput input { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point A")]
        public ValueInput pointA { get; private set; }

        [DoNotSerialize]
        [PortLabel("Point B")]
        public ValueInput pointB { get; private set; }

        
        [DoNotSerialize]
        [PortLabel("Duration")]
        public ValueInput duration { get; private set; }
        
        /// <summary>
        /// The target object used when setting the value.
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Target")]
        [PortLabelHidden]
        public ValueOutput targetOutput { get; private set; }

        [DoNotSerialize]
        public ControlOutput assigned { get; private set; }

        [DoNotSerialize]
        public ControlOutput done { get; private set; }

        
        protected override void Definition()
        {
            base.Definition();
            assign = ControlInput(nameof(assign), Assign);

            assigned = ControlOutput(nameof(assigned));
            done = ControlOutput(nameof(done));
            Succession(assign, assigned);
            Succession(assign, done);
            
            if (member.requiresTarget)
            {
                Requirement(target, assign);
            }

            input = ValueInput(member.type, nameof(input));
            Requirement(input, assign);
            
            if (member.allowsNull)
            {
                input.AllowsNull();
            }
            input.SetDefaultValue(member.type.PseudoDefault());
            
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

        protected override bool IsMemberValid(Member member)
        {
            return member.isAccessor && member.isSettable;
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

        protected void StartNewInterpolation(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);
            data.running = true;
            
            data.duration = flow.GetValue<float>(this.duration);
            data.pointA = flow.GetValue<Vector2>(pointA); 
            data.pointB = flow.GetValue<Vector2>(pointB);
            data.time = 0f;
            
            data.endValue = flow.GetConvertedValue(input);
            data.startValue = member.Get(flow.GetValue(this.target, member.targetType));
            data.lastSetValue = data.startValue;
        }

        protected bool SetValue(Flow flow, Data data, object newValue)
        {
            var target = flow.GetValue(this.target, member.targetType);

            var currentSetValue = member.Get(target);
            if (!InterpolateHelper.AreValuesEqual(data.lastSetValue, currentSetValue))
                return false;
            
            member.Set(target, newValue);

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
        
        #region Analytics

        public override AnalyticsIdentifier GetAnalyticsIdentifier()
        {
            var aid = new AnalyticsIdentifier
            {
                Identifier = $"{member.targetType.FullName}.{member.name}(Interpolate)",
                Namespace = member.targetType.Namespace,
            };
            aid.Hashcode = aid.Identifier.GetHashCode();
            return aid;
        }

        #endregion
    }
    
}
