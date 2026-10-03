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

        /// <summary>Compiled input sockets: where each input's value lives in the engine's <see cref="VariantStore"/>.</summary>
        internal readonly Dictionary<string, InputBinding> inputs = new();

        public BehaviourEngineNode(BehaviourEngine engine, Node node)
        {
            this.node = node;
            this.engine = engine;

            for (int i = 0; i < node.values.Count; i++)
            {
                values.Add(node.values[i].id, node.values[i]);
                Util.Log($"Adding value {node.values[i].id} to BehaviourGraphNode {node.type}");
            }

            for (int i = 0; i < node.flows.Count; i++)
            {
                flows.Add(node.flows[i].fromSocket, node.flows[i]);
                Util.Log($"Adding flow {node.flows[i].fromSocket} to BehaviourGraphNode {node.type}");
            }

            for (int i = 0; i < node.configuration.Count; i++)
            {
                configuration.Add(node.configuration[i].id, node.configuration[i]);
                Util.Log($"Adding config {node.configuration[i].id} to BehaviourGraphNode {node.type}");
            }

            Util.Log($"Finished creating BehaviourGraphNode {node.type}");
        }

        public void ValidateAndExecute(string socket)
        {
            Execute(socket, Validate(socket));
        }

        protected virtual void Execute(string socket, ValidationResult validationResult) { }

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
            var hasFlow = flows.TryGetValue(outputSocketName, out Flow flow);

            if (hasFlow)
                engine.ExecuteFlow(flow);

            return hasFlow;
        }

        /// <summary>Evaluates an input value socket. False when the socket is missing or its source produced no value.</summary>
        public bool TryEvaluateValue(string valueId, out Variant value)
        {
            if (!inputs.TryGetValue(valueId, out var binding))
            {
                value = default;
                return false;
            }

            value = engine.Read(in binding);
            return !value.isNone;
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