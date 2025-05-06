using System;

namespace UnityGLTF.Interactivity.Schema
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class InputSocketDescriptionAttribute : Attribute
    {
        public string[] supportedTypes;
        
        public InputSocketDescriptionAttribute(params string[] supportedTypes)
        {
            this.supportedTypes = supportedTypes;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class InputSocketDescriptionWithTypeDependencyFromOtherPortAttribute : InputSocketDescriptionAttribute
    {
        public TypeRestriction typeRestriction;
        
        public InputSocketDescriptionWithTypeDependencyFromOtherPortAttribute(string sameAsSocket,
            params string[] supportedTypes) : base(supportedTypes)
        {
            typeRestriction = TypeRestriction.SameAsInputPort(sameAsSocket);
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class OutputSocketDescriptionAttribute : Attribute
    {
        public string[] supportedTypes;
        
        public OutputSocketDescriptionAttribute( params string[] supportedTypes)
        {
            this.supportedTypes = supportedTypes;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class OutputSocketDescriptionWithTypeDependencyFromInputAttribute : OutputSocketDescriptionAttribute
    {
        public ExpectedType expectedType;
        public OutputSocketDescriptionWithTypeDependencyFromInputAttribute(string sameTypeAsInputSocket) : base()
        {
            expectedType = ExpectedType.FromInputSocket(sameTypeAsInputSocket);
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class FlowInSocketDescriptionAttribute : Attribute
    {
    }
    
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class FlowOutSocketDescriptionAttribute : Attribute
    {
    }
    
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
    public class ConfigDescriptionAttribute : Attribute
    {
        public object defaultValue = null;
        
        public ConfigDescriptionAttribute(object defaultValue = null)
        {
            this.defaultValue = defaultValue;
        }
    }
}