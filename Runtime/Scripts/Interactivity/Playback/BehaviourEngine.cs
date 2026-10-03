using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Where an input value socket reads from: a store slot, filled either by an inline constant
    /// (<see cref="source"/> is null) or by the retained output <see cref="socket"/> of <see cref="source"/>.
    /// </summary>
    internal readonly struct InputBinding
    {
        /// <summary>An input whose source node is not part of the engine; it never has a value.</summary>
        public static readonly InputBinding Unbound = new(-1, null, null);

        public readonly int slot;
        public readonly BehaviourEngineNode source;
        public readonly string socket;

        public InputBinding(int slot, BehaviourEngineNode source, string socket)
        {
            this.slot = slot;
            this.source = source;
            this.socket = socket;
        }
    }

    /// <summary>A compiled output flow: the flow and the engine node and (interned) input socket it activates.</summary>
    internal readonly struct FlowTarget
    {
        public readonly Flow flow;
        public readonly BehaviourEngineNode node;
        public readonly string socket;
        /// <summary>False when the target operation has no input flow socket with this id, so activating it is a no-op.</summary>
        public readonly bool isInputFlow;

        public FlowTarget(Flow flow, BehaviourEngineNode node, string socket)
        {
            this.flow = flow;
            this.node = node;
            this.socket = socket;
            isInputFlow = node != null && node.HasInputFlow(socket);
        }
    }

    public class BehaviourEngine : IDisposable
    {
        public Graph graph { get; private set; }
        public readonly Dictionary<Node, BehaviourEngineNode> engineNodes = new();
        public GLTFInteractivityAnimationWrapper animationWrapper { get; private set; }
        public readonly PointerInterpolationManager pointerInterpolationManager = new();
        public readonly VariableInterpolationManager variableInterpolationManager;
        public readonly NodeDelayManager nodeDelayManager = new();

        /// <summary>False when the graph was rejected; a rejected graph never executes.</summary>
        public bool isValid { get; private set; }

        /// <summary>Native storage for every variable, inline constant and retained output. Null for a rejected graph.</summary>
        public VariantStore store { get; private set; }

        /// <summary>Engine clock in seconds. Replaceable for tests.</summary>
        public Func<double> timeSource = () => Time.timeAsDouble;
        public double time => timeSource();

        /// <summary>
        /// Incremented whenever a node with flow sockets executes. Output values of nodes are retained
        /// (cached) until this changes, as required by the "Sockets" section of the spec.
        /// </summary>
        public int flowEpoch { get; private set; }

        public event Action onStart;

        public event Action<RayArgs> onSelect;
        public event Action<RayArgs> onHoverIn;
        public event Action<RayArgs> onHoverOut;
        /// <summary>Runs before onTick so event handlers observe the updated animation state.</summary>
        public event Action onAnimationUpdate;
        public event Action onTick;

        /// <summary>
        /// Raised after the graph's own event/receive nodes for every custom event occurrence. The dictionary is built
        /// only when this event has subscribers, so subscribing costs an allocation per event.
        /// </summary>
        public event Action<int, Dictionary<string, IProperty>> onCustomEventFired;
        public event Action<Flow> onFlowTriggered;

        /// <summary>Raised for event/receive nodes with the occurrence's values and which of them were provided.</summary>
        internal event Action<int, Variant[], bool[]> customEventFired;

        public PointerResolver pointerResolver { get; private set; }

        // Lifecycle event state shared by every event/onStart and event/onTick node.
        public Ref startEvent { get; private set; }
        public Ref tickEvent { get; private set; }
        public Ref lastCustomEvent { get; private set; }
        public float timeSinceStart { get; private set; } = float.NaN;
        public float timeSinceLastTick { get; private set; } = float.NaN;

        private bool _started;
        private bool _hasTicked;
        private double _firstTickTime;
        private double _lastTickTime;
        private int _nextEventId = 1;
        /// <summary>Stop flags of an event occurrence whose handlers are running.</summary>
        private struct DispatchState
        {
            public Ref eventRef;
            public bool transitivelyStopped;
            public bool immediatelyStopped;
        }

        // Event occurrences being dispatched, innermost last; nested custom events push more. Stop flags only matter
        // while their occurrence dispatches, so they are dropped with it and nothing accumulates across ticks.
        private readonly List<DispatchState> _dispatching = new();

        private readonly List<(int slot, Value value)> _constants = new();
        private readonly Dictionary<(BehaviourEngineNode node, string socket), int> _outputSlots = new();
        private int[] _variableSlots = Array.Empty<int>();

        // Custom event payloads, one frame per nesting depth so a handler can send events without clobbering its caller's values.
        private readonly List<Variant[]> _payloadValues = new();
        private readonly List<bool[]> _payloadProvided = new();
        private int _payloadDepth;

        /// <summary>
        /// Validates and compiles the graph. Touches no Unity API, so it may run off the main thread (see <see cref="CreateAsync"/>);
        /// <see cref="StartPlayback"/> must run on the main thread.
        /// </summary>
        public BehaviourEngine(Graph graph, PointerResolver pointerResolver)
        {
            this.graph = graph;
            this.pointerResolver = pointerResolver;
            variableInterpolationManager = new VariableInterpolationManager(this);

            isValid = graph != null && graph.isValid && GraphValidator.Validate(graph);

            if (!isValid)
            {
                Debug.LogWarning($"KHR_interactivity graph rejected:\n{(graph == null ? "no graph" : string.Join("\n", graph.errors))}");
                return;
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                engineNodes.Add(graph.nodes[i], NodeRegistry.CreateBehaviourEngineNode(this, graph.nodes[i]));
            }

            Compile();
        }

        /// <summary>
        /// Validates and compiles the graph on a worker thread. Call <see cref="StartPlayback"/> (or <see cref="Tick"/>)
        /// on the main thread once the task completes.
        /// </summary>
        public static Task<BehaviourEngine> CreateAsync(Graph graph, PointerResolver pointerResolver, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => new BehaviourEngine(graph, pointerResolver), cancellationToken);
        }

        /// <summary>
        /// Assigns a store slot to every variable, inline constant and consumed output socket,
        /// and resolves every output flow to the engine node it activates.
        /// </summary>
        private void Compile()
        {
            var slotCount = 0;

            _variableSlots = new int[graph.variables.Count];
            for (int i = 0; i < _variableSlots.Length; i++)
                _variableSlots[i] = slotCount++;

            foreach (var kvp in engineNodes)
            {
                var node = kvp.Value;
                var values = kvp.Key.values;

                for (int i = 0; i < values.Count; i++)
                {
                    var value = values[i];

                    if (value.node == null)
                    {
                        var slot = slotCount++;
                        _constants.Add((slot, value));
                        node.inputBindings[i] = new InputBinding(slot, null, null);
                        continue;
                    }

                    if (!engineNodes.TryGetValue(value.node, out var source))
                        continue;

                    // Interned so the source's GetOutputValue switch matches its ConstStrings cases by reference.
                    var socket = BehaviourEngineNode.Intern(value.socket);
                    var key = (source, socket);
                    if (!_outputSlots.TryGetValue(key, out var outputSlot))
                    {
                        outputSlot = slotCount++;
                        _outputSlots.Add(key, outputSlot);
                    }

                    node.inputBindings[i] = new InputBinding(outputSlot, source, socket);
                }

                var flows = kvp.Key.flows;

                for (int i = 0; i < flows.Count; i++)
                {
                    engineNodes.TryGetValue(flows[i].toNode, out var target);
                    node.flowTargets[i] = new FlowTarget(flows[i], target, BehaviourEngineNode.Intern(flows[i].toSocket));
                }
            }

            store = new VariantStore(slotCount);

            for (int i = 0; i < _variableSlots.Length; i++)
                graph.variables[i].Bind(store, _variableSlots[i]);

            WriteConstants();
        }

        private void WriteConstants()
        {
            for (int i = 0; i < _constants.Count; i++)
                store[_constants[i].slot] = Variant.FromProperty(_constants[i].value.property);
        }

        /// <summary>Frees the native value storage. Variables keep their last values in <see cref="Variable.property"/>.</summary>
        public void Dispose()
        {
            if (store == null || store.isDisposed)
                return;

            for (int i = 0; i < _variableSlots.Length; i++)
                graph.variables[i].Unbind(store);

            store.Dispose();
        }

        public void StartPlayback()
        {
            if (!isValid || _started)
                return;

            _started = true;

            // Done here rather than in the constructor because the animation wrapper is attached afterwards,
            // and because the constructor may have run off the main thread.
            ResolveStaticReferences();
            WriteConstants();

            foreach (var node in engineNodes.Values)
                node.OnEngineReady();

            startEvent = CreateEventReference();
            BeginDispatch(startEvent);

            try
            {
                onStart?.Invoke();
            }
            finally
            {
                EndDispatch();
            }
        }

        public void Tick()
        {
            if (!isValid)
                return;

            // The first tick must come after every event/onStart activation.
            if (!_started)
                StartPlayback();

            var now = time;

            pointerInterpolationManager.OnTick(now);
            variableInterpolationManager.OnTick(now);
            nodeDelayManager.OnTick(now);
            onAnimationUpdate?.Invoke();

            if (!_hasTicked)
            {
                _hasTicked = true;
                _firstTickTime = now;
                timeSinceStart = 0f;
                timeSinceLastTick = float.NaN;
            }
            else
            {
                timeSinceStart = (float)(now - _firstTickTime);
                timeSinceLastTick = (float)(now - _lastTickTime);
            }

            _lastTickTime = now;
            tickEvent = CreateEventReference();
            BeginDispatch(tickEvent);

            try
            {
                onTick?.Invoke();
            }
            finally
            {
                EndDispatch();
            }
        }

        public void Select(in RayArgs args)
        {
            onSelect?.Invoke(args);
        }

        public void HoverIn(in RayArgs args)
        {
            onHoverIn?.Invoke(args);
        }

        public void HoverOut(in RayArgs args)
        {
            onHoverOut?.Invoke(args);
        }

        public void ExecuteFlow(Flow flow)
        {
            Assert.IsNotNull(flow.toNode);

            ExecuteFlow(new FlowTarget(flow, engineNodes[flow.toNode], BehaviourEngineNode.Intern(flow.toSocket)));
        }

        internal void ExecuteFlow(in FlowTarget target)
        {
            // Same failure as the dictionary lookup this replaces, for a flow to a node outside the engine.
            if (target.node == null)
                throw new KeyNotFoundException($"Flow target node {target.flow.toNode?.type} is not part of this engine.");

            if (!target.isInputFlow)
                return;

            flowEpoch++;
            onFlowTriggered?.Invoke(target.flow);

            target.node.ValidateAndExecute(target.socket);
        }

        /// <summary>
        /// Called by flow/for and flow/while when they self-activate their "in" flow, which is also
        /// a flow activation for the purposes of output value retention.
        /// </summary>
        public void NotifySelfActivation()
        {
            flowEpoch++;
        }

        /// <summary>
        /// Reads an input: a constant, or the source's output retained for the current flow epoch.
        /// Writes through <paramref name="value"/> rather than returning it, which saves copies of the 68-byte <see cref="Variant"/>.
        /// </summary>
        /// <returns>False when the input is unbound or has no value.</returns>
        internal bool TryRead(in InputBinding binding, out Variant value)
        {
            if (binding.slot < 0)
            {
                value = default;
                return false;
            }

            if (binding.source == null || store.GetStamp(binding.slot) == flowEpoch)
            {
                value = store[binding.slot];
                return !value.isNone;
            }

            var epoch = flowEpoch;

            try
            {
                value = binding.source.GetOutputValue(binding.socket);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                value = default;
            }

            store[binding.slot] = value;
            store.SetStamp(binding.slot, epoch);
            return !value.isNone;
        }

        /// <summary>The retained value of an output socket, computing it if no input consumes that socket.</summary>
        internal Variant ReadOutput(BehaviourEngineNode node, string socket)
        {
            if (_outputSlots.TryGetValue((node, socket), out var slot))
            {
                TryRead(new InputBinding(slot, node, socket), out var value);
                return value;
            }

            return node.GetOutputValue(socket);
        }

        public Variant GetVariable(int variableIndex) => store[_variableSlots[variableIndex]];

        public void SetVariable(int variableIndex, in Variant value) => store[_variableSlots[variableIndex]] = value;

        /// <summary>
        /// Rents the payload frame for a custom event occurrence; fill it, call <see cref="FireCustomEvent(int, Variant[], bool[])"/>,
        /// then <see cref="ReturnEventPayload"/>. Frames are reused, so this allocates only the first time a nesting depth is reached.
        /// </summary>
        public void RentEventPayload(int valueCount, out Variant[] values, out bool[] provided)
        {
            if (_payloadDepth == _payloadValues.Count)
            {
                _payloadValues.Add(new Variant[Math.Max(valueCount, 4)]);
                _payloadProvided.Add(new bool[Math.Max(valueCount, 4)]);
            }
            else if (_payloadValues[_payloadDepth].Length < valueCount)
            {
                _payloadValues[_payloadDepth] = new Variant[valueCount];
                _payloadProvided[_payloadDepth] = new bool[valueCount];
            }

            values = _payloadValues[_payloadDepth];
            provided = _payloadProvided[_payloadDepth];
            Array.Clear(provided, 0, provided.Length);
            _payloadDepth++;
        }

        public void ReturnEventPayload()
        {
            if (_payloadDepth > 0)
                _payloadDepth--;
        }

        /// <summary>Raises a custom event from inside the graph (event/send). Values are indexed like the event's definition.</summary>
        public void FireCustomEvent(int eventIndex, Variant[] values, bool[] provided)
        {
            if (eventIndex < 0 || eventIndex >= graph.customEvents.Count)
                return;

            // Restore afterwards so receivers of an outer event still see its reference
            // when a handler sends a nested event.
            var previous = lastCustomEvent;
            lastCustomEvent = CreateEventReference();
            BeginDispatch(lastCustomEvent);

            try
            {
                customEventFired?.Invoke(eventIndex, values, provided);

                if (onCustomEventFired != null)
                    onCustomEventFired.Invoke(eventIndex, ToDictionary(eventIndex, values, provided));
            }
            finally
            {
                EndDispatch();
                lastCustomEvent = previous;
            }
        }

        /// <summary>Raises a custom event with boxed values. Allocates; prefer the payload overload on hot paths.</summary>
        public void FireCustomEvent(int eventIndex, Dictionary<string, IProperty> outValues = null)
        {
            if (eventIndex < 0 || eventIndex >= graph.customEvents.Count)
                return;

            var definition = graph.customEvents[eventIndex].values;
            var count = definition?.Count ?? 0;

            RentEventPayload(count, out var values, out var provided);

            try
            {
                for (int i = 0; outValues != null && i < count; i++)
                {
                    if (outValues.TryGetValue(definition[i].id, out var p) && p != null)
                    {
                        values[i] = Variant.FromProperty(p);
                        provided[i] = !values[i].isNone;
                    }
                }

                FireCustomEvent(eventIndex, values, provided);
            }
            finally
            {
                ReturnEventPayload();
            }
        }

        private Dictionary<string, IProperty> ToDictionary(int eventIndex, Variant[] values, bool[] provided)
        {
            var definition = graph.customEvents[eventIndex].values;
            var result = new Dictionary<string, IProperty>();

            for (int i = 0; definition != null && i < definition.Count; i++)
            {
                if (provided[i])
                    result[definition[i].id] = values[i].ToProperty();
            }

            return result;
        }

        /// <summary>
        /// Raises a custom event from the external environment using its external id.
        /// Values not provided are reset to their initial or type-default values by event/receive.
        /// </summary>
        public bool SendExternalEvent(string id, Dictionary<string, IProperty> values = null)
        {
            if (!isValid || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < graph.customEvents.Count; i++)
            {
                if (graph.customEvents[i].id == id)
                {
                    FireCustomEvent(i, values);
                    return true;
                }
            }

            return false;
        }

        public Ref CreateEventReference()
        {
            return Ref.Event(_nextEventId++);
        }

        public bool IsEventReference(Ref r)
        {
            return r.kind == RefKind.Event && r.id > 0 && r.id < _nextEventId;
        }

        /// <summary>
        /// event/stopPropagation. Transitive activations (scene graph propagation) are always cancelled;
        /// <paramref name="immediate"/> also cancels handlers for the same event that have not run yet.
        /// Has no effect on an event whose dispatch has finished, since nothing is left to cancel.
        /// </summary>
        public void StopPropagation(Ref eventRef, bool immediate)
        {
            if (!IsEventReference(eventRef))
                return;

            var index = IndexOfDispatch(eventRef);

            if (index < 0)
                return;

            var state = _dispatching[index];
            state.transitivelyStopped = true;
            state.immediatelyStopped |= immediate;
            _dispatching[index] = state;
        }

        /// <summary>True while the event is dispatching after a stopPropagation with stopImmediate.</summary>
        public bool IsImmediatelyStopped(Ref eventRef)
        {
            var index = IndexOfDispatch(eventRef);
            return index >= 0 && _dispatching[index].immediatelyStopped;
        }

        /// <summary>True while the event is dispatching after any stopPropagation.</summary>
        public bool IsTransitivelyStopped(Ref eventRef)
        {
            var index = IndexOfDispatch(eventRef);
            return index >= 0 && _dispatching[index].transitivelyStopped;
        }

        private void BeginDispatch(Ref eventRef)
        {
            _dispatching.Add(new DispatchState { eventRef = eventRef });
        }

        private void EndDispatch()
        {
            _dispatching.RemoveAt(_dispatching.Count - 1);
        }

        private int IndexOfDispatch(Ref eventRef)
        {
            // Innermost first: that is usually the event being stopped. The list is as deep as event nesting.
            for (int i = _dispatching.Count - 1; i >= 0; i--)
            {
                if (_dispatching[i].eventRef == eventRef)
                    return i;
            }

            return -1;
        }

        public bool TryGetPointer(string pointerString, BehaviourEngineNode engineNode, out IPointer pointer)
        {
            try
            {
                pointer = pointerResolver.GetPointer(pointerString, engineNode);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); // Feeding in a really malformed pointerString could throw maybe?

                pointer = PointerHelpers.InvalidPointer();
            }
            return !pointer.invalid;
        }

        public void SetAnimationWrapper(GLTFInteractivityAnimationWrapper wrapper, Animation animation)
        {
            animationWrapper = wrapper;
            wrapper.SetData(this, animation);
            pointerResolver.CreateAnimationPointers(wrapper);
        }

        /// <summary>
        /// Converts an animation reference to an animation index, or returns false if the reference
        /// is not a valid glTF animation reference for this asset.
        /// </summary>
        public bool TryGetAnimationIndex(Ref animation, out int index)
        {
            index = -1;

            if (animation.kind != RefKind.Gltf || animation.collectionId != RefCollections.Animations)
                return false;

            if (animationWrapper == null || !animationWrapper.IsValidAnimationIndex(animation.id))
                return false;

            index = animation.id;
            return true;
        }

        public void PlayAnimation(in AnimationPlayData data)
        {
            if (!HasAnimationWrapper())
                return;

            animationWrapper.PlayAnimation(data);
        }

        public void StopAnimation(int index)
        {
            if (!HasAnimationWrapper())
                return;

            animationWrapper.StopAnimation(index);
        }

        public void StopAnimationAt(int index, float stopTime, Action callback)
        {
            if (!HasAnimationWrapper())
                return;

            animationWrapper.StopAnimationAt(index, stopTime, callback);
        }

        public bool HasAnimationWrapper()
        {
            if (animationWrapper == null)
            {
                Util.LogWarning("Tried to play an animation on a glb that has no animations.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Static glTF references (variable initial values, inline values, event initial values)
        /// that do not resolve against the asset become null references.
        /// </summary>
        private void ResolveStaticReferences()
        {
            if (pointerResolver == null)
                return;

            for (int i = 0; i < graph.variables.Count; i++)
            {
                var variable = graph.variables[i];
                variable.property = ResolveStaticReference(variable.property);
                variable.initialValue = ResolveStaticReference(variable.initialValue);
            }

            for (int i = 0; i < graph.customEvents.Count; i++)
            {
                var values = graph.customEvents[i].values;

                for (int j = 0; values != null && j < values.Count; j++)
                {
                    values[j].property = ResolveStaticReference(values[j].property);
                }
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                var values = graph.nodes[i].values;

                for (int j = 0; j < values.Count; j++)
                {
                    if (values[j].node == null)
                        values[j].property = ResolveStaticReference(values[j].property);
                }
            }
        }

        private IProperty ResolveStaticReference(IProperty property)
        {
            if (property is Property<Ref> r && r.value.kind == RefKind.Gltf && !RefExistsInAsset(r.value))
                return new Property<Ref>(Ref.Null);

            return property;
        }

        private bool RefExistsInAsset(Ref r)
        {
            if (r.collectionId == RefCollections.Animations && animationWrapper != null)
                return animationWrapper.IsValidAnimationIndex(r.id);

            return pointerResolver.RefExists(r);
        }
    }
}
