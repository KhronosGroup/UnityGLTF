using System;

namespace UnityGLTF.Plugins
{
    /// <summary>
    /// Marks a plugin as non-ratified. This is used to indicate that the extension is not yet part of the official glTF specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NonRatifiedPluginAttribute : Attribute
    {
        public string tooltip;
        
        public NonRatifiedPluginAttribute(string tooltip = null)
        {
            this.tooltip = tooltip;
        }
    }
    
    /// <summary>
    /// Marks a plugin as experiental.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExperimentalPluginAttribute : Attribute
    {
        public string tooltip;
        
        public ExperimentalPluginAttribute(string tooltip = null)
        {
            this.tooltip = tooltip;
        }
    }
    
    /// <summary>
    /// Marks a plugin as unsupported in the current version of Unity. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UnsupportedUnityVersionPluginAttribute : Attribute
    {
        public string requiredVersion;
        
        public UnsupportedUnityVersionPluginAttribute(string requiredVersion = null)
        {
            this.requiredVersion = requiredVersion;
        }
    }

}