using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
    internal static class NonHumanoidSetup
    {
        internal static Avatar AddAvatarToGameObject(GameObject gameObject, bool flipForward, string RootNode)
        {
            var previousRotation = gameObject.transform.rotation;
            if (flipForward)
                gameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);

            Avatar avatar = AvatarBuilder.BuildGenericAvatar(gameObject, RootNode);
            avatar.name = gameObject.name + "Avatar";

            if (flipForward)
                gameObject.transform.rotation = previousRotation;

            if (!avatar.isValid || avatar.isHuman)
            {
                Object.DestroyImmediate(avatar);
                return null;
            }

            var animator = gameObject.GetComponent<Animator>();
            if (animator) animator.avatar = avatar;
            return avatar;
        }
    }
}
