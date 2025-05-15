using System;

namespace UnityGLTF.Plugins
{
    /// <summary>
    /// Marks a plugin as non-ratified. This is used to indicate that the extension is not yet part of the official glTF specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NonRatifiedPluginAttribute : Attribute
    {
        public string toolTip;
        
        public NonRatifiedPluginAttribute(string toolTip = null)
        {
            this.toolTip = toolTip;
        }
    }
}