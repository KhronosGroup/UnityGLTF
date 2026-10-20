using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    public class KHR_interactivity
    {
        public List<Graph> graphs { get; set; } = new();
        public int defaultGraphIndex { get; set; }

        /// <summary>False when the extension object itself is invalid, e.g. the "graph" property is out of range.</summary>
        public bool isValid { get; set; } = true;
        public List<string> errors { get; set; } = new();

        /// <summary>
        /// Returns the default graph if the extension object and that graph are both valid.
        /// Implementations may treat the asset as having no interactivity otherwise.
        /// </summary>
        public bool TryGetDefaultGraph(out Graph graph)
        {
            graph = null;

            if (!isValid || defaultGraphIndex < 0 || defaultGraphIndex >= graphs.Count)
                return false;

            graph = graphs[defaultGraphIndex];
            return graph.isValid;
        }
    }

    public class Declaration
    {
        public string op { get; set; }
        public string extension { get; set; }
        public List<ValueSocket> inputValueSockets { get; set; }
        public List<ValueSocket> outputValueSockets { get; set; }
    }

    public class ValueSocket
    {
        public ValueSocket(string name, int type)
        {
            this.name = name;
            this.type = type;
        }

        public string name { get; set; }
        public int type { get; set; }
    }

    public class Metadata
    {
        public double positionX { get; set; }
        public double positionY { get; set; }
    }

    public class Variable
    {
        public string id { get; set; }
        public IProperty initialValue { get; set; }

        private IProperty _property;
        private VariantStore _store;
        private int _slot = -1;

        /// <summary>
        /// The variable's current value. While a running engine owns the variable, the value lives in the engine's
        /// <see cref="VariantStore"/>; reading this boxes a copy, so playback code uses the store directly.
        /// </summary>
        public IProperty property
        {
            get => isBound ? _store[_slot].ToProperty() : _property;
            set
            {
                _property = value;
                if (isBound)
                    _store[_slot] = Variant.FromProperty(value);
            }
        }

        internal bool isBound => _store != null && !_store.isDisposed;

        internal void Bind(VariantStore store, int slot)
        {
            _store = store;
            _slot = slot;
            store[slot] = Variant.FromProperty(_property);
        }

        /// <summary>Copies the runtime value back so <see cref="property"/> keeps working after the engine is disposed.</summary>
        internal void Unbind(VariantStore store)
        {
            if (_store != store)
                return;

            if (!store.isDisposed)
                _property = store[_slot].ToProperty();

            _store = null;
            _slot = -1;
        }
    }

    public class Configuration
    {
        public string id { get; set; }
        public IProperty property { get; set; }
        public bool parsedSuccessfully { get; set; }
        /// <summary>The raw JSON "value" array, kept for operation-specific validation. Null for graphs built in code.</summary>
        [JsonIgnore] public JArray raw { get; set; }
    }

    public class InteractivityType
    {
        public string signature { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TypeExtensions extensions { get; set; }
    }

    public class TypeExtensions
    {
        public AMZN_Interactivity_String AMZN_interactivity_string { get; set; }
    }

    public class AMZN_Interactivity_String
    {
    }
}