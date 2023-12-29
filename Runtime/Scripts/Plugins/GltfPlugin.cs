using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GltfPlugin: ScriptableObject
    {
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual string HelpUrl => "";
        public bool Enabled { get; set; } = true;
    }
}