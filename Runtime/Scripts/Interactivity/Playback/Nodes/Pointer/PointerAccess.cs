using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Shared Object Model access for pointer/get, pointer/set and pointer/interpolate:
    /// the parsed template, the configured type, and runtime resolution of the effective pointer.
    /// Resolutions are cached per distinct set of template parameter values, so steady-state access
    /// builds no strings and allocates nothing.
    /// </summary>
    internal sealed class PointerAccess
    {
        public readonly PointerTemplate template;
        /// <summary>The value type selected by the "type" configuration.</summary>
        public readonly Type type;
        public readonly int typeIndex;

        private readonly int[] _ints;
        private readonly Ref[] _refs;
        /// <summary>Input index of each template parameter on the owning node.</summary>
        private readonly int[] _parameterInputs;

        /// <summary>Pointers whose validity depends on runtime state, so a cached resolution is re-checked on every access.</summary>
        private enum Liveness : byte { Static, Delay, Event }

        private readonly struct Resolution
        {
            public readonly IPointer pointer;
            public readonly string effectivePointer;
            public readonly bool valid;
            public readonly Liveness liveness;
            public readonly Ref reference;

            public Resolution(IPointer pointer, string effectivePointer, bool valid, Liveness liveness, Ref reference)
            {
                this.pointer = pointer;
                this.effectivePointer = effectivePointer;
                this.valid = valid;
                this.liveness = liveness;
                this.reference = reference;
            }
        }

        /// <summary>Up to four template parameter values packed into a dictionary key.</summary>
        private readonly struct ParameterKey : IEquatable<ParameterKey>
        {
            private readonly long _a, _b, _c, _d;

            public ParameterKey(long a, long b, long c, long d) { _a = a; _b = b; _c = c; _d = d; }

            public bool Equals(ParameterKey o) => _a == o._a && _b == o._b && _c == o._c && _d == o._d;
            public override bool Equals(object obj) => obj is ParameterKey k && Equals(k);
            public override int GetHashCode() => HashCode.Combine(_a, _b, _c, _d);
        }

        private const int MaxCachedParameters = 4;
        /// <summary>Beyond this many distinct parameter sets, resolutions are no longer cached, bounding memory.</summary>
        private const int MaxCacheEntries = 256;
        private readonly Dictionary<ParameterKey, Resolution> _cache = new();

        /// <summary>
        /// Templates of the exact form /extensions/KHR_interactivity/delays/{x} or .../events/{x}: their target is the
        /// reference itself, so they resolve with a live validity check and one shared pointer instead of the cache.
        /// </summary>
        private readonly Liveness _virtualReference;
        // Boxed once here; assigning a pointer struct to IPointer per call would allocate.
        private readonly IPointer _virtualPointer;
        private Ref _virtualCurrent;

        private PointerAccess(BehaviourEngineNode node, PointerTemplate template, Type type, int typeIndex)
        {
            this.template = template;
            this.type = type;
            this.typeIndex = typeIndex;
            _ints = new int[template.parameters.Count];
            _refs = new Ref[template.parameters.Count];
            _parameterInputs = new int[template.parameters.Count];

            for (int i = 0; i < _parameterInputs.Length; i++)
                _parameterInputs[i] = node.GetInputIndex(template.parameters[i].id);

            if (template.parameters.Count == 1 && template.parameters[0].isReference)
            {
                var t = template.template;
                var tail = "{" + template.parameters[0].id + "}";

                if (t == "/extensions/KHR_interactivity/delays/" + tail)
                    _virtualReference = Liveness.Delay;
                else if (t == "/extensions/KHR_interactivity/events/" + tail)
                    _virtualReference = Liveness.Event;

                if (_virtualReference != Liveness.Static)
                    _virtualPointer = new ReadOnlyPointer<Ref>(() => _virtualCurrent);
            }
        }

        /// <summary>Reads and checks the node configuration. Graph validation guarantees success for loaded graphs.</summary>
        public static bool TryCreate(BehaviourEngineNode node, out PointerAccess access)
        {
            access = null;

            if (!node.TryGetConfig(ConstStrings.POINTER, out string pointer) || !PointerTemplate.TryParse(pointer, out var template, out _))
                return false;

            var types = node.engine.graph.types;

            if (!node.TryGetConfig(ConstStrings.TYPE, out int typeIndex) || typeIndex < 0 || typeIndex >= types.Count)
                return false;

            access = new PointerAccess(node, template, Helpers.GetSystemType(types[typeIndex]), typeIndex);
            return true;
        }

        /// <summary>
        /// Evaluates template parameters and resolves the effective pointer. Fails when an integer parameter is
        /// negative, a reference parameter is null, the pointer does not resolve, or its type differs from <see cref="type"/>.
        /// </summary>
        public bool TryResolve(BehaviourEngineNode node, out IPointer pointer, out string effectivePointer)
        {
            pointer = default;
            effectivePointer = null;

            var parameters = template.parameters;

            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].isReference)
                {
                    if (!node.TryEvaluateValue(_parameterInputs[i], out Ref r) || r.isNull)
                        return false;
                    _refs[i] = r;
                }
                else
                {
                    if (!node.TryEvaluateValue(_parameterInputs[i], out int v) || v < 0)
                        return false;
                    _ints[i] = v;
                }
            }

            if (_virtualReference != Liveness.Static)
            {
                var r = _refs[0];
                _virtualCurrent = r;
                pointer = _virtualPointer;
                var valid = _virtualReference == Liveness.Delay
                    ? r.kind == RefKind.Delay && node.engine.nodeDelayManager.IsActive(r)
                    : r.kind == RefKind.Event && node.engine.IsEventReference(r);
                return valid && type == typeof(Ref);
            }

            if (parameters.Count > MaxCachedParameters)
            {
                var resolution = Resolve(node);
                return Finish(node, resolution, out pointer, out effectivePointer);
            }

            var key = MakeKey();

            if (!_cache.TryGetValue(key, out var cached))
            {
                cached = Resolve(node);

                if (_cache.Count < MaxCacheEntries)
                    _cache.Add(key, cached);
            }

            return Finish(node, cached, out pointer, out effectivePointer);
        }

        private bool Finish(BehaviourEngineNode node, in Resolution resolution, out IPointer pointer, out string effectivePointer)
        {
            pointer = resolution.pointer;
            effectivePointer = resolution.effectivePointer;

            return resolution.liveness switch
            {
                Liveness.Delay => node.engine.nodeDelayManager.IsActive(resolution.reference) && type == typeof(Ref),
                Liveness.Event => node.engine.IsEventReference(resolution.reference) && type == typeof(Ref),
                _ => resolution.valid,
            };
        }

        private ParameterKey MakeKey()
        {
            Span<long> k = stackalloc long[MaxCachedParameters];
            var parameters = template.parameters;

            for (int i = 0; i < parameters.Count; i++)
            {
                k[i] = parameters[i].isReference
                    ? ((long)_refs[i].kind << 56) | ((long)(_refs[i].collectionId & 0xFFFFFF) << 32) | (uint)_refs[i].id
                    : _ints[i];
            }

            return new ParameterKey(k[0], k[1], k[2], k[3]);
        }

        private Resolution Resolve(BehaviourEngineNode node)
        {
            if (!template.TryGenerate(_ints, _refs, out var effective))
                return new Resolution(null, null, false, Liveness.Static, Ref.Null);

            // Delay and event virtual properties are valid only while the referenced object is active.
            var liveness = Liveness.Static;
            var reference = Ref.Null;
            var parameters = template.parameters;

            if (parameters.Count == 1 && parameters[0].isReference)
            {
                if (effective.StartsWith("/extensions/KHR_interactivity/delays/", StringComparison.Ordinal) && _refs[0].kind == RefKind.Delay)
                    liveness = Liveness.Delay;
                else if (effective.StartsWith("/extensions/KHR_interactivity/events/", StringComparison.Ordinal) && _refs[0].kind == RefKind.Event)
                    liveness = Liveness.Event;

                if (liveness != Liveness.Static)
                {
                    var r = _refs[0];
                    reference = r;
                    return new Resolution(new ReadOnlyPointer<Ref>(() => r), effective, true, liveness, reference);
                }
            }

            if (!node.engine.TryGetPointer(effective, node, out var pointer))
                return new Resolution(pointer, effective, false, Liveness.Static, Ref.Null);

            if (pointer is ObjectIndexPointer objectIndex && type == typeof(Ref))
                return new Resolution(objectIndex.AsRef(), effective, true, Liveness.Static, Ref.Null);

            return new Resolution(pointer, effective, PointerHelpers.GetSpecType(pointer) == type, Liveness.Static, Ref.Null);
        }
    }
}
