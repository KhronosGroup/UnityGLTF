using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	internal static class HumanoidSetup
	{
	    private static MethodInfo _SetupHumanSkeleton;

	    internal static Avatar AddAvatarToGameObject(GameObject gameObject, bool flipForward)
	    {
		    HumanDescription description = AvatarUtils.CreateHumanDescription(gameObject);
		    var bones = description.human;
		    SetupHumanSkeleton(gameObject, ref bones, out var skeletonBones, out var hasTranslationDoF);
		    description.human = bones;
		    description.skeleton = skeletonBones;
		    description.hasTranslationDoF = hasTranslationDoF;

		    var previousRotation = gameObject.transform.rotation;
		    if (flipForward)
				gameObject.transform.rotation *= Quaternion.Euler(0,180,0);
		    
		    Avatar avatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
		    avatar.name = "Avatar";
		    
		    if (flipForward)
			    gameObject.transform.rotation = previousRotation;

		    if (!avatar.isValid || !avatar.isHuman)
		    {
			    Object.DestroyImmediate(avatar);
			    return null;
		    }

		    var animator = gameObject.GetComponent<Animator>();
		    if (animator) animator.avatar = avatar;
		    return avatar;
	    }

	    private static void SetupHumanSkeleton(
		    GameObject modelPrefab,
		    ref HumanBone[] humanBoneMappingArray,
		    out SkeletonBone[] skeletonBones,
		    out bool hasTranslationDoF)
	    {
		    _SetupHumanSkeleton = typeof(AvatarSetupTool).GetMethod(nameof(SetupHumanSkeleton), (BindingFlags)(-1));
		    skeletonBones = Array.Empty<SkeletonBone>();
		    hasTranslationDoF = false;

		    _SetupHumanSkeleton?.Invoke(null, new object[]
		    {
			    modelPrefab,
			    humanBoneMappingArray,
			    skeletonBones,
			    hasTranslationDoF
		    });
	    }


	    // AvatarSetupTools
	    // AvatarBuilder.BuildHumanAvatar
	    // AvatarConfigurationStage.CreateStage
	    // AssetImporterTabbedEditor
	    // ModelImporterRigEditor

#if TESTING
	    [MenuItem("Tools/Copy Hierarchy Array")]
	    static void _Copy(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var sb = new System.Text.StringBuilder();

		    void Traverse(Transform tr)
		    {
			    sb.AppendLine(tr.name);
			    foreach (Transform child in tr)
			    {
				    Traverse(child);
			    }
		    }

		    Traverse(gameObject.transform);
		    EditorGUIUtility.systemCopyBuffer = sb.ToString();
	    }

	    [MenuItem("Tools/Setup Humanoid")]
	    static void _Do(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    // SetupHumanSkeleton(go, ref humanBoneMappingArray, out var skeletonBones, out var hasTranslationDoF);
			AddAvatarToGameObject(gameObject);
	    }

	    [MenuItem("Tools/Open Avatar Editor")]
	    static void _OpenEditor(MenuCommand command)
	    {
		    var gameObject = Selection.activeGameObject;
		    var avatar = gameObject.GetComponent<Animator>().avatar;
		    var e = (AvatarEditor) Editor.CreateEditor(avatar, typeof(AvatarEditor));
		    e.m_CameFromImportSettings = true;
		    Selection.activeObject = e;
		    e.SwitchToEditMode();
	    }
#endif
	}
}
