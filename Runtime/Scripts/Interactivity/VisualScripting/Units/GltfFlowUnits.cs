using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VisualScripting
{
    /// <summary>Handle of one delay started by <see cref="GltfSetDelay"/>, used by <see cref="GltfCancelDelay"/>.</summary>
    public sealed class GltfDelayHandle
    {
        public float endTime;
        public bool cancelled;
    }

    /// <summary>
    /// KHR_interactivity flow/setDelay: every activation schedules its own "done" after the duration.
    /// "cancel" cancels all pending delays of this node, a single delay is cancelled with <see cref="GltfCancelDelay"/>.
    /// </summary>
    [UnitCategory("Time")]
    [UnitTitle("Set Delay (glTF)")]
    public class GltfSetDelay : Unit, IGraphElementWithData, IGraphEventListener
    {
        public sealed class Data : IGraphElementData
        {
            public readonly List<GltfDelayHandle> pending = new List<GltfDelayHandle>();
            public GltfDelayHandle lastDelay;
            public bool isListening;
            public Delegate update;
        }

        [DoNotSerialize, PortLabelHidden] public ControlInput enter { get; private set; }
        [DoNotSerialize] public ControlInput cancel { get; private set; }
        [DoNotSerialize] public ValueInput duration { get; private set; }
        [DoNotSerialize, PortLabelHidden] public ControlOutput exit { get; private set; }
        [DoNotSerialize] public ControlOutput err { get; private set; }
        [DoNotSerialize] public ControlOutput done { get; private set; }
        [DoNotSerialize] public ValueOutput lastDelay { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), Enter);
            cancel = ControlInput(nameof(cancel), Cancel);
            duration = ValueInput(nameof(duration), 1f);
            exit = ControlOutput(nameof(exit));
            err = ControlOutput(nameof(err));
            done = ControlOutput(nameof(done));
            lastDelay = ValueOutput<object>(nameof(lastDelay), flow => flow.stack.GetElementData<Data>(this).lastDelay);

            Requirement(duration, enter);
            Succession(enter, exit);
            Succession(enter, err);
        }

        private ControlOutput Enter(Flow flow)
        {
            var seconds = flow.GetValue<float>(duration);
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return err;

            var data = flow.stack.GetElementData<Data>(this);
            var handle = new GltfDelayHandle { endTime = Time.time + seconds };
            data.pending.Add(handle);
            data.lastDelay = handle;
            return exit;
        }

        private ControlOutput Cancel(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);
            foreach (var handle in data.pending)
                handle.cancelled = true;
            data.pending.Clear();
            return null;
        }

        public IGraphElementData CreateData() => new Data();

        public void StartListening(GraphStack stack)
        {
            var data = stack.GetElementData<Data>(this);
            if (data.isListening)
                return;

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
                return;

            var hook = new EventHook(EventHooks.Update, stack.machine);
            EventBus.Unregister(hook, data.update);
            stack.ClearReference();
            data.update = null;
            data.isListening = false;
        }

        public bool IsListening(GraphPointer pointer) => pointer.GetElementData<Data>(this).isListening;

        private void TriggerUpdate(GraphReference reference)
        {
            var data = reference.GetElementData<Data>(this);
            if (data.pending.Count == 0)
                return;

            var now = Time.time;
            var due = data.pending.FindAll(handle => handle.endTime <= now);
            data.pending.RemoveAll(handle => handle.endTime <= now);
            foreach (var handle in due)
            {
                if (handle.cancelled)
                    continue;
                using (var flow = Flow.New(reference))
                    flow.Invoke(done);
            }
        }
    }

    /// <summary>KHR_interactivity flow/cancelDelay: cancels one delay started by <see cref="GltfSetDelay"/>.</summary>
    [UnitCategory("Time")]
    [UnitTitle("Cancel Delay (glTF)")]
    public class GltfCancelDelay : Unit
    {
        [DoNotSerialize, PortLabelHidden] public ControlInput enter { get; private set; }
        [DoNotSerialize] public ValueInput delay { get; private set; }
        [DoNotSerialize, PortLabelHidden] public ControlOutput exit { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), flow =>
            {
                if (flow.GetValue(delay) is GltfDelayHandle handle)
                    handle.cancelled = true;
                return exit;
            });
            delay = ValueInput<object>(nameof(delay)).AllowsNull();
            exit = ControlOutput(nameof(exit));
            Requirement(delay, enter);
            Succession(enter, exit);
        }
    }

    /// <summary>KHR_interactivity flow/throttle: passes at most one activation per duration.</summary>
    [UnitCategory("Control")]
    [UnitTitle("Throttle (glTF)")]
    public class GltfThrottle : Unit, IGraphElementWithData
    {
        public sealed class Data : IGraphElementData
        {
            public bool hasPassed;
            public float lastPassTime;
            public float lastRemainingTime;
        }

        [DoNotSerialize, PortLabelHidden] public ControlInput enter { get; private set; }
        [DoNotSerialize] public ControlInput reset { get; private set; }
        [DoNotSerialize] public ValueInput duration { get; private set; }
        [DoNotSerialize, PortLabelHidden] public ControlOutput exit { get; private set; }
        [DoNotSerialize] public ControlOutput err { get; private set; }
        [DoNotSerialize] public ValueOutput lastRemainingTime { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), Enter);
            reset = ControlInput(nameof(reset), flow =>
            {
                var data = flow.stack.GetElementData<Data>(this);
                data.hasPassed = false;
                data.lastRemainingTime = 0f;
                return null;
            });
            duration = ValueInput(nameof(duration), 1f);
            exit = ControlOutput(nameof(exit));
            err = ControlOutput(nameof(err));
            lastRemainingTime = ValueOutput(nameof(lastRemainingTime), flow => flow.stack.GetElementData<Data>(this).lastRemainingTime);

            Requirement(duration, enter);
            Succession(enter, exit);
            Succession(enter, err);
        }

        private ControlOutput Enter(Flow flow)
        {
            var seconds = flow.GetValue<float>(duration);
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return err;

            var data = flow.stack.GetElementData<Data>(this);
            var now = Time.time;
            if (data.hasPassed && now - data.lastPassTime < seconds)
            {
                data.lastRemainingTime = seconds - (now - data.lastPassTime);
                return null;
            }

            data.hasPassed = true;
            data.lastPassTime = now;
            data.lastRemainingTime = 0f;
            return exit;
        }

        public IGraphElementData CreateData() => new Data();
    }

    /// <summary>KHR_interactivity flow/doN: passes the first n activations until reset.</summary>
    [UnitCategory("Control")]
    [UnitTitle("Do N (glTF)")]
    public class GltfDoN : Unit, IGraphElementWithData
    {
        public sealed class Data : IGraphElementData
        {
            public int count;
        }

        [DoNotSerialize, PortLabelHidden] public ControlInput enter { get; private set; }
        [DoNotSerialize] public ControlInput reset { get; private set; }
        [DoNotSerialize] public ValueInput n { get; private set; }
        [DoNotSerialize, PortLabelHidden] public ControlOutput exit { get; private set; }
        [DoNotSerialize] public ValueOutput currentCount { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), flow =>
            {
                var data = flow.stack.GetElementData<Data>(this);
                if (data.count >= flow.GetValue<int>(n))
                    return null;
                data.count++;
                return exit;
            });
            reset = ControlInput(nameof(reset), flow =>
            {
                flow.stack.GetElementData<Data>(this).count = 0;
                return null;
            });
            n = ValueInput(nameof(n), 1);
            exit = ControlOutput(nameof(exit));
            currentCount = ValueOutput(nameof(currentCount), flow => flow.stack.GetElementData<Data>(this).count);

            Requirement(n, enter);
            Succession(enter, exit);
        }

        public IGraphElementData CreateData() => new Data();
    }

    /// <summary>KHR_interactivity flow/multiGate: each activation fires the next (or a random) output that has
    /// not fired yet; with loop enabled it starts over once all outputs fired.</summary>
    [UnitCategory("Control")]
    [UnitTitle("Multi Gate (glTF)")]
    public class GltfMultiGate : Unit, IGraphElementWithData
    {
        public sealed class Data : IGraphElementData
        {
            public bool[] used;
            public int lastIndex = -1;
        }

        [SerializeAs(nameof(outputCount))]
        private int _outputCount = 2;

        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Outputs")]
        public int outputCount
        {
            get => _outputCount;
            set => _outputCount = Mathf.Clamp(value, 1, 32);
        }

        [Serialize, Inspectable] public bool isRandom { get; set; }
        [Serialize, Inspectable] public bool isLoop { get; set; }

        [DoNotSerialize, PortLabelHidden] public ControlInput enter { get; private set; }
        [DoNotSerialize] public ControlInput reset { get; private set; }
        [DoNotSerialize] public List<ControlOutput> outputs { get; } = new List<ControlOutput>();
        [DoNotSerialize] public ValueOutput lastIndex { get; private set; }

        protected override void Definition()
        {
            enter = ControlInput(nameof(enter), Enter);
            reset = ControlInput(nameof(reset), flow =>
            {
                var data = flow.stack.GetElementData<Data>(this);
                data.used = null;
                data.lastIndex = -1;
                return null;
            });
            outputs.Clear();
            for (int i = 0; i < outputCount; i++)
            {
                var output = ControlOutput(i.ToString());
                outputs.Add(output);
                Succession(enter, output);
            }
            lastIndex = ValueOutput(nameof(lastIndex), flow => flow.stack.GetElementData<Data>(this).lastIndex);
        }

        private ControlOutput Enter(Flow flow)
        {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.used == null || data.used.Length != outputCount)
                data.used = new bool[outputCount];

            var free = new List<int>();
            for (int i = 0; i < outputCount; i++)
                if (!data.used[i]) free.Add(i);

            if (free.Count == 0)
            {
                if (!isLoop)
                    return null;
                Array.Clear(data.used, 0, data.used.Length);
                for (int i = 0; i < outputCount; i++) free.Add(i);
            }

            var index = isRandom ? free[UnityEngine.Random.Range(0, free.Count)] : free[0];
            data.used[index] = true;
            data.lastIndex = index;
            return outputs[index];
        }

        public IGraphElementData CreateData() => new Data();
    }

    /// <summary>KHR_interactivity integer bit operations (math/and, or, xor, not, asr, lsl, clz, ctz, popcnt).</summary>
    [UnitCategory("Math/Integer")]
    [UnitTitle("Integer Bitwise (glTF)")]
    public class GltfIntBitwise : Unit
    {
        public enum Operation { And, Or, Xor, Not, ShiftRight, ShiftLeft, CountLeadingZeros, CountTrailingZeros, PopCount }

        [Serialize, Inspectable, UnitHeaderInspectable]
        public Operation operation { get; set; } = Operation.And;

        [DoNotSerialize] public ValueInput a { get; private set; }
        [DoNotSerialize] public ValueInput b { get; private set; }
        [DoNotSerialize] public ValueOutput value { get; private set; }

        public bool HasSecondInput => operation <= Operation.Xor || operation == Operation.ShiftRight || operation == Operation.ShiftLeft;

        protected override void Definition()
        {
            a = ValueInput(nameof(a), 0);
            if (HasSecondInput)
                b = ValueInput(nameof(b), 0);
            value = ValueOutput(nameof(value), Compute).Predictable();
            Requirement(a, value);
            if (b != null)
                Requirement(b, value);
        }

        private int Compute(Flow flow)
        {
            var x = flow.GetValue<int>(a);
            var y = b != null ? flow.GetValue<int>(b) : 0;
            switch (operation)
            {
                case Operation.And: return x & y;
                case Operation.Or: return x | y;
                case Operation.Xor: return x ^ y;
                case Operation.Not: return ~x;
                case Operation.ShiftRight: return x >> (y & 31);
                case Operation.ShiftLeft: return x << (y & 31);
                case Operation.CountLeadingZeros: return LeadingZeros((uint)x);
                case Operation.CountTrailingZeros: return x == 0 ? 32 : TrailingZeros((uint)x);
                case Operation.PopCount: return PopCount((uint)x);
                default: throw new UnexpectedEnumValueException<Operation>(operation);
            }
        }

        private static int LeadingZeros(uint x)
        {
            if (x == 0) return 32;
            int n = 0;
            while ((x & 0x80000000u) == 0) { n++; x <<= 1; }
            return n;
        }

        private static int TrailingZeros(uint x)
        {
            int n = 0;
            while ((x & 1u) == 0) { n++; x >>= 1; }
            return n;
        }

        private static int PopCount(uint x)
        {
            int n = 0;
            while (x != 0) { n += (int)(x & 1u); x >>= 1; }
            return n;
        }
    }
}
