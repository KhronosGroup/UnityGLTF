using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
	internal static class HumanoidSetup
	{
		internal static Avatar AddAvatarToGameObject(GameObject gameObject, bool flipForward)
		{
			var previousRotation = gameObject.transform.rotation;
			if (flipForward)
				gameObject.transform.rotation *= Quaternion.Euler(0, 180, 0);

			try
			{
				var description = AvatarUtils.CreateHumanDescription(gameObject);
				var namedBones = description.human;
				SetupHumanSkeleton(gameObject, namedBones, out var human, out var skeleton, out var hasTranslationDoF);
				description.human = human;
				description.skeleton = skeleton;
				description.hasTranslationDoF = hasTranslationDoF;

				var avatar = BuildHumanAvatar(gameObject, description);
				if (!avatar)
				{
					// Fall back to the bone names only and the model's own pose
					description.human = namedBones;
					description.skeleton = Array.Empty<SkeletonBone>();
					description.hasTranslationDoF = false;
					avatar = BuildHumanAvatar(gameObject, description);
				}
				if (!avatar) return null;

				var animator = gameObject.GetComponent<Animator>();
				if (animator) animator.avatar = avatar;
				return avatar;
			}
			finally
			{
				if (flipForward)
					gameObject.transform.rotation = previousRotation;
			}
		}

		private static Avatar BuildHumanAvatar(GameObject gameObject, HumanDescription description)
		{
			var avatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
			avatar.name = gameObject.name + "Avatar";
			if (avatar.isValid && avatar.isHuman) return avatar;

			Object.DestroyImmediate(avatar);
			return null;
		}

		/// <summary>
		/// Does what AvatarSetupTool.SetupHumanSkeleton does for the model importer (its signature differs between Unity
		/// versions, so it's rebuilt from the AvatarSetupTool helpers): completes the bone mapping with Unity's auto
		/// mapping and creates the skeleton pose, corrected to a T-pose. Works on a copy, so the imported hierarchy keeps its pose.
		/// Bones mapped by name (see <see cref="AvatarUtils"/>) take priority over the auto mapping.
		/// </summary>
		private static void SetupHumanSkeleton(GameObject gameObject, HumanBone[] namedBones,
			out HumanBone[] human, out SkeletonBone[] skeleton, out bool hasTranslationDoF)
		{
			hasTranslationDoF = false;
			var copy = Object.Instantiate(gameObject);
			// Skeleton bones are matched by name, so the root must keep its name (no "(Clone)")
			copy.name = gameObject.name;
			try
			{
				var isBiped = AvatarBipedMapper.IsBiped(copy.transform, null);
				AvatarSetupTool.SampleBindPose(copy);

				// GetModelBones uses the bone weights of skinned meshes, which aren't readable when Read/Write is disabled.
				// Then all transforms are treated as bones.
				var meshesReadable = copy.GetComponentsInChildren<SkinnedMeshRenderer>(true)
					.All(r => !r.sharedMesh || r.sharedMesh.isReadable);
				var modelBones = AvatarSetupTool.GetModelBones(copy.transform, !meshesReadable, null);
				var autoMapping = isBiped
					? AvatarBipedMapper.MapBones(copy.transform)
					: AvatarAutoMapper.MapBones(copy.transform, modelBones);

				var mapping = new Dictionary<string, string>();
				foreach (var bone in namedBones)
				{
					if (!mapping.ContainsKey(bone.humanName))
						mapping[bone.humanName] = bone.boneName;
				}
				var mappedTransforms = new HashSet<string>(mapping.Values);
				foreach (var pair in autoMapping)
				{
					var humanName = HumanTrait.BoneName[pair.Key];
					if (!pair.Value || mapping.ContainsKey(humanName) || mappedTransforms.Contains(pair.Value.name)) continue;
					mapping[humanName] = pair.Value.name;
					mappedTransforms.Add(pair.Value.name);
				}
				human = mapping.Select(m => new HumanBone
				{
					humanName = m.Key,
					boneName = m.Value,
					limit = new HumanLimit { useDefaultValues = true },
				}).ToArray();

				var humanBones = AvatarSetupTool.GetHumanBones(mapping, modelBones);
				if (isBiped)
				{
					AvatarBipedMapper.BipedPose(copy, humanBones);
					hasTranslationDoF = true;
				}
				else
				{
					// Start from whichever is closer to a T-pose, the bind pose or the model's pose, then enforce a T-pose
					var bindPoseError = AvatarSetupTool.GetPoseError(humanBones);
					AvatarSetupTool.CopyPose(copy, gameObject);
					if (bindPoseError < AvatarSetupTool.GetPoseError(humanBones))
						AvatarSetupTool.SampleBindPose(copy);
					AvatarSetupTool.MakePoseValid(humanBones);
				}

				skeleton = AvatarSetupTool.GetSkeletonBones(copy.transform);
			}
			finally
			{
				Object.DestroyImmediate(copy);
			}
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
