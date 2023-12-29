using UnityEngine;

namespace UnityGLTF.Plugins
{
    public abstract class GltfPlugin: ScriptableObject
    {
        public abstract string DisplayName { get; }
        public bool Enabled { get; set; } = true;
    }
}