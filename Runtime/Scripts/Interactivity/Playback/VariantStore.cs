using System;
#if !INTERACTIVITY_MANAGED_STORE
using Unity.Collections;
#endif

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Value storage for one running graph: every variable, inline constant and retained node output has a slot.
    /// Backed by native memory so loading and running graphs adds nothing to the managed heap, and so the values
    /// can later be handed to jobs. Each slot also has a retention stamp: the flow epoch its value was computed in.
    /// Define INTERACTIVITY_MANAGED_STORE to use managed arrays instead (for running the runtime outside Unity).
    /// </summary>
    public sealed class VariantStore : IDisposable
    {
#if INTERACTIVITY_MANAGED_STORE
        private Variant[] _values;
        private int[] _stamps;
#else
        private NativeArray<Variant> _values;
        private NativeArray<int> _stamps;
#endif

        public int length { get; }
        public bool isDisposed { get; private set; }

        public VariantStore(int length)
        {
            this.length = length;
#if INTERACTIVITY_MANAGED_STORE
            _values = new Variant[length];
            _stamps = new int[length];
#else
            _values = new NativeArray<Variant>(Math.Max(length, 1), Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _stamps = new NativeArray<int>(Math.Max(length, 1), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
#endif
            for (int i = 0; i < length; i++)
                _stamps[i] = -1;
        }

        public Variant this[int slot]
        {
            get => _values[slot];
            set => _values[slot] = value;
        }

        /// <summary>The flow epoch the slot's value was computed in, or -1 if never computed.</summary>
        public int GetStamp(int slot) => _stamps[slot];

        public void SetStamp(int slot, int epoch) => _stamps[slot] = epoch;

#if !INTERACTIVITY_MANAGED_STORE
        /// <summary>The native value array, for jobs. Valid until the engine is disposed.</summary>
        public NativeArray<Variant> values => _values;
#endif

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
#if INTERACTIVITY_MANAGED_STORE
            _values = null;
            _stamps = null;
#else
            if (_values.IsCreated) _values.Dispose();
            if (_stamps.IsCreated) _stamps.Dispose();
#endif
        }
    }
}
