using System;
using System.Collections.Generic;
using System.Reflection;
using UnityGLTF.Interactivity.Schema;

namespace UnityGLTF.Interactivity.Schema
{
    
    public class GltfInteractivityNodeSchema
    {
        public virtual string Op { get; set; } = string.Empty;

        public virtual string Extension { get; protected set; } = null;

        public IReadOnlyDictionary<string, ConfigDescriptor> Configuration { get => _configuration; }
        public IReadOnlyDictionary<string, FlowSocketDescriptor> InputFlowSockets { get => _inputFlowSockets; } 
        public IReadOnlyDictionary<string, FlowSocketDescriptor> OutputFlowSockets {get => _outputFlowSockets; } 
        public IReadOnlyDictionary<string, InputValueSocketDescriptor> InputValueSockets {get => _inputValueSockets;}
        public IReadOnlyDictionary<string, OutputValueSocketDescriptor> OutputValueSockets {get => _outputValueSockets;} 

        protected Dictionary<string, ConfigDescriptor> _configuration = new();
        protected Dictionary<string, FlowSocketDescriptor> _inputFlowSockets = new();
        protected Dictionary<string, FlowSocketDescriptor> _outputFlowSockets = new();
        protected Dictionary<string, InputValueSocketDescriptor> _inputValueSockets = new();
        protected Dictionary<string, OutputValueSocketDescriptor> _outputValueSockets = new();
        
        public MetaDataEntry[] MetaDatas {get; protected set;}
        
        private static Dictionary<System.Type, GltfInteractivityNodeSchema> _schemaInstances =
            new Dictionary<System.Type, GltfInteractivityNodeSchema>();

        public static GltfInteractivityNodeSchema GetSchema<TSchema>() where TSchema : GltfInteractivityNodeSchema, new()
        {
            var type = typeof(TSchema);
            if (_schemaInstances.ContainsKey(type))
                return _schemaInstances[type];
            
            var schema = new TSchema();
            _schemaInstances.Add(type, schema);
            return schema;
        }
        
        public static GltfInteractivityNodeSchema GetSchema(Type schemaType)
        {
            if (_schemaInstances.ContainsKey(schemaType))
                return _schemaInstances[schemaType];
            
            var schema = (GltfInteractivityNodeSchema)Activator.CreateInstance(schemaType);
            _schemaInstances.Add(schemaType, schema);
            return schema;
        }

        public void CreateDescriptorsFromAttributes(bool clearExisting = false)
        {
            if (clearExisting)
            {
                _configuration.Clear();
                _inputFlowSockets.Clear();
                _outputFlowSockets.Clear();
                _inputValueSockets.Clear();
                _outputValueSockets.Clear();
            }
            
            var fields = new List<FieldInfo>();
            
            void AddFields(Type type)
            {
                fields.AddRange(type.GetFields());
                if (type.BaseType != null)
                    AddFields(type.BaseType);
            }
            AddFields(GetType());
            
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(string))
                    return;
                
                var fieldValue = field.GetValue(this) as string;
                
                var attributes = field.GetCustomAttributes(false);
                foreach (var attribute in attributes)
                {
                    
                    if (attribute is ConfigDescriptionAttribute configDescription)
                    {
                        _configuration.Add(fieldValue, new ConfigDescriptor { defaultValue = configDescription.defaultValue });
                    }
                    else if (attribute is OutputSocketDescriptionWithTypeDependencyFromInputAttribute outputSocketDescriptionWithExpectedType)
                    {
                        var newOut = new OutputValueSocketDescriptor
                        {
                            SupportedTypes = outputSocketDescriptionWithExpectedType.supportedTypes,
                            expectedType = outputSocketDescriptionWithExpectedType.expectedType
                        };
                        if (newOut.SupportedTypes == null || newOut.SupportedTypes.Length == 0)
                            newOut.SupportedTypes = GltfTypes.allTypes;

                        _outputValueSockets.Add(fieldValue, newOut);
                    }
                    else if (attribute is InputSocketDescriptionWithTypeDependencyFromOtherPortAttribute inputSocketDescriptionWithTypeRestriction)
                    {
                        var newIn = new InputValueSocketDescriptor
                        {
                            SupportedTypes = inputSocketDescriptionWithTypeRestriction.supportedTypes,
                            typeRestriction = inputSocketDescriptionWithTypeRestriction.typeRestriction
                        };
                        if (newIn.SupportedTypes == null || newIn.SupportedTypes.Length == 0)
                            newIn.SupportedTypes = GltfTypes.allTypes;
                        
                        _inputValueSockets.Add(fieldValue, newIn);
                    }
                    else if (attribute is InputSocketDescriptionAttribute inputSocketDescription)
                    {
                        var newIn = new InputValueSocketDescriptor
                        {
                            SupportedTypes = inputSocketDescription.supportedTypes
                        };
                        
                        if (newIn.SupportedTypes == null || newIn.SupportedTypes.Length == 0)
                            newIn.SupportedTypes = GltfTypes.allTypes;
                        if (newIn.SupportedTypes != null && newIn.SupportedTypes.Length == 1)
                            newIn.typeRestriction = TypeRestriction.LimitToType(newIn.SupportedTypes[0]);
                        
                        _inputValueSockets.Add(fieldValue, newIn);
                    }
                    else if (attribute is OutputSocketDescriptionAttribute outputSocketDescription)
                    {
                        var newOut = new OutputValueSocketDescriptor
                        {
                            SupportedTypes = outputSocketDescription.supportedTypes
                        };

                        if (newOut.SupportedTypes == null || newOut.SupportedTypes.Length == 0)
                            newOut.SupportedTypes = GltfTypes.allTypes;
                        else if (newOut.SupportedTypes != null && newOut.SupportedTypes.Length == 1)
                        {
                            newOut.expectedType = ExpectedType.GtlfType(newOut.SupportedTypes[0]);
                        }
                        _outputValueSockets.Add(fieldValue, newOut);
                    }
                    else if (attribute is FlowInSocketDescriptionAttribute)
                    {
                        _inputFlowSockets.Add(fieldValue, new FlowSocketDescriptor());
                    }
                    else if (attribute is FlowOutSocketDescriptionAttribute)
                    {
                        _outputFlowSockets.Add(fieldValue, new FlowSocketDescriptor());
                    }
                }
            }
        }
        
        public GltfInteractivityNodeSchema()
        {
            MetaDatas = new MetaDataEntry[] { };
            CreateDescriptorsFromAttributes();
        }
        
        public class MetaDataEntry
        {
            public string key;
            public string value;
        }
        
        /// <summary> Describes configuration parameters for the node.</summary>
        public class ConfigDescriptor 
        {
            // The expected data type of the configuration parameter field.
            public string Type = string.Empty;
            public object defaultValue = null;
        }

        /// <summary>
        /// Describes sockets that control the flow of execution before/after this node.
        /// </summary>
        public class FlowSocketDescriptor 
        {
        }

        /// <summary>
        /// Describes sockets that import/export values before/after the node's execution.
        /// </summary>
        public class ValueSocketDescriptor 
        {
            // List of data types that can be imported/exported by this value socket.
            public string[] SupportedTypes = { };
        }
        
        public class InputValueSocketDescriptor : ValueSocketDescriptor
        {
            public TypeRestriction typeRestriction = null;
        }
        
        /// <summary>
        /// Describes sockets that import/export values before/after the node's execution.
        /// </summary>
        public class OutputValueSocketDescriptor : ValueSocketDescriptor
        {
            // List of data types that can be imported/exported by this value socket.
            public ExpectedType expectedType = null;
        }
        
    }
}
