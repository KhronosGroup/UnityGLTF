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

        public Dictionary<string, ConfigDescriptor> Configuration { get; set; } = new();
        public Dictionary<string, FlowSocketDescriptor> InputFlowSockets { get; set; } = new();
        public Dictionary<string, FlowSocketDescriptor> OutputFlowSockets {get; set;} = new();
        public Dictionary<string, InputValueSocketDescriptor> InputValueSockets {get; set;} = new();
        public Dictionary<string, OutputValueSocketDescriptor> OutputValueSockets {get; set;} = new();
        
        public MetaDataEntry[] MetaDatas {get; protected set;}

        public void CreateDescriptorsFromAttributes(bool clearExisting = false)
        {
            if (clearExisting)
            {
                Configuration.Clear();
                InputFlowSockets.Clear();
                OutputFlowSockets.Clear();
                InputValueSockets.Clear();
                OutputValueSockets.Clear();
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
                        Configuration.Add(fieldValue, new ConfigDescriptor( ));
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

                        OutputValueSockets.Add(fieldValue, newOut);
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
                        
                        InputValueSockets.Add(fieldValue, newIn);
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
                        
                        InputValueSockets.Add(fieldValue, newIn);
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
                        OutputValueSockets.Add(fieldValue, newOut);
                    }
                    else if (attribute is FlowInSocketDescriptionAttribute)
                    {
                        InputFlowSockets.Add(fieldValue, new FlowSocketDescriptor());
                    }
                    else if (attribute is FlowOutSocketDescriptionAttribute)
                    {
                        OutputFlowSockets.Add(fieldValue, new FlowSocketDescriptor());
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
