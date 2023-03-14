using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BKUnity;
using UnityEditor;
using UnityEngine;

public class HumanoidSetup : MonoBehaviour
{
    // Start is called before the first frame update
    private static MethodInfo _SetupHumanSkeleton;

    // AvatarSetupTools
    // AvatarBuilder.BuildHumanAvatar
    // AvatarConfigurationStage.CreateStage
    // AssetImporterTabbedEditor
    // ModelImporterRigEditor

    [MenuItem("Tools/Copy Hierarchy Array")]
    static void _Copy(MenuCommand command)
    {
	    var gameObject = Selection.activeGameObject;
	    var sb = new StringBuilder();

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

    internal static Avatar AddAvatarToGameObject(GameObject gameObject)
    {
	    HumanDescription description = AvatarUtils.CreateHumanDescription(gameObject);
	    var bones = description.human;
	    SetupHumanSkeleton(gameObject, ref bones, out var skeletonBones, out var hasTranslationDoF);
	    description.human = bones;
	    description.skeleton = skeletonBones;
	    description.hasTranslationDoF = hasTranslationDoF;

	    Avatar avatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
	    avatar.name = "Avatar";
	    Debug.Log(avatar);

	    gameObject.GetComponent<Animator>().avatar = avatar;
	    return avatar;
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

    private static void SetupHumanSkeleton(
	    GameObject modelPrefab,
	    ref HumanBone[] humanBoneMappingArray,
	    out SkeletonBone[] skeletonBones,
	    out bool hasTranslationDoF)
    {
	    _SetupHumanSkeleton = typeof(AvatarSetupTool).GetMethod(nameof(SetupHumanSkeleton), (BindingFlags)(-1));
	    skeletonBones = new SkeletonBone[0];
	    hasTranslationDoF = false;

	    _SetupHumanSkeleton.Invoke(null, new object[]
	    {
		    modelPrefab,
		    humanBoneMappingArray,
		    skeletonBones,
		    hasTranslationDoF
	    });

	    Debug.Log(skeletonBones.Length);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
