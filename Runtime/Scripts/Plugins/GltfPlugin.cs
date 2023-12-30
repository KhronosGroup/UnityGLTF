using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GltfPlugin: ScriptableObject
    {
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public virtual string HelpUrl => null;
        public bool Enabled { get; set; } = true;
        public virtual bool EnabledByDefault => true;
        public virtual bool AlwaysEnabled => false;
        public virtual string Warning => null;
    }
}