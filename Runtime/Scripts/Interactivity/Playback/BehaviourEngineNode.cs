using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback
{
    // Made partial so that extension methods could be added elsewhere without bloating this code file with a bunch of overloaded methods.
    // Did not use actual extension methods for this since they would require the use of "this" keyword to access if used in a node script.
    public abstract partial class BehaviourEngineNode
    {
        public enum ValidationResult
        {
            Valid = 0,
            InvalidConfiguration = 1,
            InvalidFlow = 2,
            InvalidValue = 3
        }

        public readonly BehaviourEngine engine;
        public readonly Node node;

        public readonly Dictionary<string, Value> values = new();
        public readonly Dictionary<string, Flow> flows = new();
        public readonly Dictionary<string, Configuration> configuration = new();

        // Compiled sockets, indexed like node.values and node.flows. Ids are interned so that lookups with the
        // ConstStrings constants match by reference; a small array scan is much cheaper than hashing the id.
        private readonly string[] _inputIds;
        private readonly string[] _flowIds;
        /// <summary>Where each input's value lives in the engine's <see cref="VariantStore"/>. Filled by the engine's compile step.</summary>
        internal readonly InputBinding[] inputBindings;
        /// <summary>The engine node and input socket each output flow activates. Filled by the engine's compile step.</summary>
        internal readonly FlowTarget[] flowTargets;

        private readonly bool _hasValidation;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _typesWithValidation = new();

        public BehaviourEngineNode(BehaviourEngine engine, Node node)
        {
            this.node = node;
            this.engine = engine;

            _inputIds = new string[node.values.Count];
            inputBindings = new InputBinding[node.values.Count];

            for (int i = 0; i < node.values.Count; i++)
            {
                values.Add(node.values[i].id, node.values[i]);
                _inputIds[i] = Intern(node.values[i].id);
                inputBindings[i] = InputBinding.Unbound;
                Util.Log($"Adding value {node.values[i].id} to BehaviourGraphNode {node.type}");
            }

            _flowIds = new string[node.flows.Count];
            flowTargets = new FlowTarget[node.flows.Count];

            for (int i = 0; i < node.flows.Count; i++)
            {
                flows.Add(node.flows[i].fromSocket, node.flows[i]);
                _flowIds[i] = Intern(node.flows[i].fromSocket);
                Util.Log($"Adding flow {node.flows[i].fromSocket} to BehaviourGraphNode {node.type}");
            }

            _hasValidation = _typesWithValidation.GetOrAdd(GetType(), OverridesValidation);

            for (int i = 0; i < node.configuration.Count; i++)
            {
                configuration.Add(node.configuration[i].id, node.configuration[i]);
                Util.Log($"Adding config {node.configuration[i].id} to BehaviourGraphNode {node.type}");
            }

            Util.Log($"Finished creating BehaviourGraphNode {node.type}");
        }

        public void ValidateAndExecute(string socket)
        {
            // Most node types never override the Validate* methods, so skip the virtual calls for them.
            Execute(socket, _hasValidation ? Validate(socket) : ValidationResult.Valid);
        }

        private static bool OverridesValidation(Type type)
        {
            return IsOverridden(type, nameof(ValidateConfiguration))
                || IsOverridden(type, nameof(ValidateFlows))
                || IsOverridden(type, nameof(ValidateValues));
        }

        private static bool IsOverridden(Type type, string method)
        {
            return type.GetMethod(method, new[] { typeof(string) }).DeclaringType != typeof(BehaviourEngineNode);
        }

        protected virtual void Execute(string socket, ValidationResult validationResult) { }

        /// <summary>
        /// Whether the operation has an input flow socket with this id. Flows into any other socket never execute the node,
        /// like an unconnected socket. Operations with input flows other than "in" override this.
        /// </summary>
        public virtual bool HasInputFlow(string socket) => socket == ConstStrings.IN;

        /// <summary>
        /// Computes an output value. Called at most once per output socket per flow epoch; the engine retains the result
        /// in the socket's store slot, as required by the "Sockets" section of the spec.
        /// </summary>
        public virtual Variant GetOutputValue(string socket) => default;

        /// <summary>
        /// Called on the main thread before playback starts, after the engine (possibly built off the main thread) is ready.
        /// Nodes that need Unity objects resolve them here rather than in their constructor.
        /// </summary>
        public virtual void OnEngineReady() { }

        /// <summary>The output value as retained by the engine for the current flow epoch.</summary>
        public Variant GetRetainedOutputValue(string socket) => engine.ReadOutput(this, socket);
        public virtual bool ValidateConfiguration(string socket) => true;
        public virtual bool ValidateFlows(string socket) => true;
        public virtual bool ValidateValues(string socket) => true;

        public ValidationResult Validate(string socket)
        {
            if (!ValidateConfiguration(socket))
                return ValidationResult.InvalidConfiguration;

            if (!ValidateFlows(socket))
                return ValidationResult.InvalidFlow;

            if (!ValidateValues(socket))
                return ValidationResult.InvalidValue;

            return ValidationResult.Valid;
        }

        public bool TryExecuteFlow(string outputSocketName)
        {
            return TryExecuteFlow(GetFlowIndex(outputSocketName));
        }

        /// <summary>Activates the output flow at <paramref name="flowIndex"/> (from <see cref="GetFlowIndex"/>). False when it is -1.</summary>
        public bool TryExecuteFlow(int flowIndex)
        {
            if (flowIndex < 0)
                return false;

            engine.ExecuteFlow(in flowTargets[flowIndex]);
            return true;
        }

        /// <summary>Evaluates an input value socket. False when the socket is missing or its source produced no value.</summary>
        public bool TryEvaluateValue(string valueId, out Variant value)
        {
            return TryEvaluateValue(GetInputIndex(valueId), out value);
        }

        /// <summary>Evaluates the input at <paramref name="inputIndex"/> (from <see cref="GetInputIndex"/>). False when it is -1 or produced no value.</summary>
        public bool TryEvaluateValue(int inputIndex, out Variant value)
        {
            if (inputIndex < 0)
            {
                value = default;
                return false;
            }

            return engine.TryRead(in inputBindings[inputIndex], out value);
        }

        /// <summary>
        /// Index of an input value socket for <see cref="TryEvaluateValue(int, out Variant)"/>, or -1 if the node has no such socket.
        /// Nodes that look sockets up by computed ids can resolve them once in their constructor.
        /// </summary>
        public int GetInputIndex(string valueId) => IndexOf(_inputIds, valueId);

        /// <summary>Index of an output flow socket for <see cref="TryExecuteFlow(int)"/>, or -1 if the socket is not connected.</summary>
        public int GetFlowIndex(string flowId) => IndexOf(_flowIds, flowId);

        internal static string Intern(string id) => id == null ? null : string.Intern(id);

        private static int IndexOf(string[] ids, string id)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if ((object)ids[i] == id)
                    return i;
            }

            // Ids built at runtime (e.g. from numbers) are not interned.
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id))
                    return i;
            }

            return -1;
        }

        public bool TryGetConfig<T>(string id, out T value)
        {
            // Graphs built in code may set a property without going through JSON parsing,
            // so a non-null property of the right type is accepted even if parsedSuccessfully is false.
            if (configuration.TryGetValue(id, out var config) && config.property is Property<T> typed)
            {
                value = typed.value;
                return true;
            }

            value = default;
            return false;
        }
    }
}