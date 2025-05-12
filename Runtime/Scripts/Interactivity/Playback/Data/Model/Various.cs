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
        public IProperty property { get; set; }
        public IProperty initialValue { get; set; }
    }

    public class Configuration
    {
        public string id { get; set; }
        public JArray value { get; set; }
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