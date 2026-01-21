using Unity.Mathematics;
using UnityEngine;

namespace UnityGLTF.Interactivity.Playback.Extensions
{
    public static partial class ExtensionMethods
    {
        public static bool IsAncestorOf(this Transform potentialAncestor, Transform child)
        {
            while (child.parent != null)
            {
                if (child.parent == potentialAncestor)
                    return true;

                child = child.parent;
            }

            return false;
        }
    }
}