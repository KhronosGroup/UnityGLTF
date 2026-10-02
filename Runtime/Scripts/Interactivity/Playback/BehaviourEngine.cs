using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityGLTF.Interactivity.Playback
{
    public class BehaviourEngine
    {
        public Graph graph { get; private set; }
        public readonly Dictionary<Node, BehaviourEngineNode> engineNodes = new();
        public GLTFInteractivityAnimationWrapper animationWrapper { get; private set; }
        public readonly PointerInterpolationManager pointerInterpolationManager = new();
        public readonly VariableInterpolationManager variableInterpolationManager = new();
        public readonly NodeDelayManager nodeDelayManager = new();

        /// <summary>False when the graph was rejected; a rejected graph never executes.</summary>
        public bool isValid { get; private set; }

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
        public event Action<int, Dictionary<string, IProperty>> onCustomEventFired;
        public event Action<Flow> onFlowTriggered;

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
        private readonly HashSet<Ref> _immediatelyStoppedEvents = new();
        private readonly HashSet<Ref> _transitivelyStoppedEvents = new();

        public BehaviourEngine(Graph graph, PointerResolver pointerResolver)
        {
            this.graph = graph;
            this.pointerResolver = pointerResolver;

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
        }

        public void StartPlayback()
        {
            if (!isValid || _started)
                return;

            _started = true;

            // Done here rather than in the constructor because the animation wrapper is attached afterwards.
            ResolveStaticReferences();

            startEvent = CreateEventReference();
            onStart?.Invoke();
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
            onTick?.Invoke();
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

            var node = engineNodes[flow.toNode];

            flowEpoch++;
            onFlowTriggered?.Invoke(flow);

            node.ValidateAndExecute(flow.toSocket);
        }

        /// <summary>
        /// Called by flow/for and flow/while when they self-activate their "in" flow, which is also
        /// a flow activation for the purposes of output value retention.
        /// </summary>
        public void NotifySelfActivation()
        {
            flowEpoch++;
        }

        /// <summary>Raises a custom event from inside the graph (event/send).</summary>
        public void FireCustomEvent(int eventIndex, Dictionary<string, IProperty> outValues = null)
        {
            if (eventIndex < 0 || eventIndex >= graph.customEvents.Count)
                return;

            // Restore afterwards so receivers of an outer event still see its reference
            // when a handler sends a nested event.
            var previous = lastCustomEvent;
            lastCustomEvent = CreateEventReference();

            try
            {
                onCustomEventFired?.Invoke(eventIndex, outValues);
            }
            finally
            {
                lastCustomEvent = previous;
            }
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
        /// </summary>
        public void StopPropagation(Ref eventRef, bool immediate)
        {
            if (!IsEventReference(eventRef))
                return;

            _transitivelyStoppedEvents.Add(eventRef);

            if (immediate)
                _immediatelyStoppedEvents.Add(eventRef);
        }

        public bool IsImmediatelyStopped(Ref eventRef) => _immediatelyStoppedEvents.Contains(eventRef);
        public bool IsTransitivelyStopped(Ref eventRef) => _transitivelyStoppedEvents.Contains(eventRef);

        public IProperty ParseValue(Value v)
        {
            if (v.node == null)
                return v.property;

            var node = engineNodes[v.node];
            return node.GetRetainedOutputValue(v.socket);
        }

        public IProperty GetVariableProperty(int variableIndex)
        {
            return graph.variables[variableIndex].property;
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

            if (animation.kind != RefKind.Gltf || animation.collection != "/animations")
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
            if (r.collection == "/animations" && animationWrapper != null)
                return animationWrapper.IsValidAnimationIndex(r.id);

            return pointerResolver.RefExists(r);
        }
    }
}
