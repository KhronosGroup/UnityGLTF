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
        public virtual IProperty GetOutputValue(string socket) => null;
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

        public bool TryEvaluateValue(string valueId, out IProperty value)
        {
            try
            {
                value = engine.ParseValue(values[valueId]);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                value = default;
                return false;
            }
        }

        public bool TryGetConfig<T>(string id, out T value)
        {
            if (configuration.TryGetValue(id, out var config))
            {
                try
                {
                    value = ((Property<T>)Helpers.CreateProperty(typeof(T), config.value)).value;
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }
    }
}