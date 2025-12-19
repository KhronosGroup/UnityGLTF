using System;
using UnityEngine.UIElements;

namespace UnityGLTF.Interactivity.Playback
{
    public struct NodeFlow
    {
        public string id;
        public string description;

        public NodeFlow(string id, string description)
        {
            this.id = id;
            this.description = description;
        }
    }

    public struct NodeValue
    {
        public string id;
        public string description;
        public Type[] types;

        public NodeValue(string id, string description, Type[] types)
        {
            this.id = id;
            this.description = description;
            this.types = types;
        }
    }

    public struct NodeConfiguration
    {
        public string id;
        public string description;
        public Type type;

        public NodeConfiguration(string id, string description, Type type)
        {
            this.id = id;
            this.description = description;
            this.type = type;
        }
    }

    public abstract class NodeSpecifications
    {
        private NodeFlow[] _inputFlows;
        private NodeValue[] _inputValues;
        private NodeFlow[] _outputFlows;
        private NodeValue[] _outputValues;
        private NodeConfiguration[] _configuration;

        private bool _inputsGenerated;
        private bool _outputsGenerated;
        private bool _configurationGenerated;

        public (NodeFlow[] flows, NodeValue[] values) GetInputs()
        {
            if(_inputsGenerated)
                return (_inputFlows, _inputValues);

            return GenerateInputs();
        }

        public (NodeFlow[] flows, NodeValue[] values) GetOutputs()
        {
            if (_outputsGenerated)
                return (_outputFlows, _outputValues);

            return GenerateOutputs();
        }

        public NodeConfiguration[] GetConfiguration()
        {
            if (_configurationGenerated)
                return _configuration;

            return GenerateConfiguration();
        }

        protected virtual (NodeFlow[] flows, NodeValue[] values) GenerateInputs()
        {
            return (null, null);
        }

        protected virtual (NodeFlow[] flows, NodeValue[] values) GenerateOutputs()
        {
            return (null, null);
        }

        protected virtual NodeConfiguration[] GenerateConfiguration()
        {
            return null;
        }
    }
}