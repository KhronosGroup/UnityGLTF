using System;
using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GltfPlugin: ScriptableObject
    {
        public abstract string DisplayName { get; }
        public virtual string Description => null;
        public virtual string HelpUrl => null;
        public bool Enabled { get; set; } = true;
        public virtual bool EnabledByDefault => true;
        public virtual bool AlwaysEnabled => false;
        public virtual string Warning => null;
        [Obsolete("Use a custom Editor for this ScriptableObject instead if you want to override how it looks in the Inspector.")]
        public virtual void OnGUI() { }
    }
}